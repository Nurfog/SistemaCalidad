import { useState, useEffect } from 'react';
import api from '../api/client';
import { useAuth } from '../context/AuthContext';
import {
    Search,
    Download,
    Eye,
    Plus,
    FileText,
    Clock,
    CheckCircle2,
    XCircle,
    MoreVertical,
    Send,
    CheckCircle,
    FolderPlus,
    Folder,
    ChevronRight,
    ArrowLeft,
    Trash2
} from 'lucide-react';
import DocumentModal from '../components/DocumentModal';
import CarpetaDocumentoModal from '../components/CarpetaDocumentoModal';
import '../styles/Documentos.css';

const Documentos = () => {
    const { user } = useAuth();
    const [documentos, setDocumentos] = useState([]);
    const [carpetas, setCarpetas] = useState([]); // Subcarpetas actuales
    const [loading, setLoading] = useState(true);
    const [buscar, setBuscar] = useState('');

    // Estados para Modales
    const [isModalOpen, setIsModalOpen] = useState(false);
    const [isCarpetaModalOpen, setIsCarpetaModalOpen] = useState(false);

    const [activeMenu, setActiveMenu] = useState(null);
    const [filtros, setFiltros] = useState({
        area: '',
        tipo: '',
        estado: ''
    });

    // Navegación de Carpetas (Recursiva)
    const [carpetaActual, setCarpetaActual] = useState(null); // null = Raíz
    const [breadcrumbs, setBreadcrumbs] = useState([]); // Historial de navegación

    const fetchContenido = async () => {
        setLoading(true);
        try {
            const params = {
                buscar: buscar || undefined,
                area: filtros.area || undefined,
                tipo: filtros.tipo || undefined,
                estado: filtros.estado || undefined,
                carpetaId: carpetaActual?.id || null // Filtro backend
            };

            // Optimización: Carga en paralelo
            const [resDocs, resCarpetas] = await Promise.all([
                api.get('/Documentos', { params }),
                !buscar ? api.get('/CarpetasDocumentos', { params: { parentId: carpetaActual?.id || null } }) : Promise.resolve({ data: [] })
            ]);

            setDocumentos(resDocs.data);
            setCarpetas(resCarpetas.data);

        } catch (error) {
            console.error('Error al cargar contenido:', error);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        const timer = setTimeout(() => {
            fetchContenido();
        }, 300); // Debounce más corto
        return () => clearTimeout(timer);
    }, [buscar, filtros, carpetaActual]);

    // --- Manejo de Drag and Drop ---
    const handleDragStart = (e, doc) => {
        e.dataTransfer.setData('docId', doc.id);
        e.dataTransfer.effectAllowed = 'move';
    };

    const handleDragOver = (e) => {
        e.preventDefault(); // Necesario para permitir Drop
        e.dataTransfer.dropEffect = 'move';
    };

    const handleDrop = async (e, carpetaDestino) => {
        e.preventDefault();
        const docId = e.dataTransfer.getData('docId');
        if (!docId) return;

        // Evitar mover a la misma carpeta donde ya está (visual)
        // Aunque el backend lo soportaría, es redundante.
        // Aquí asumimos mover HACIA una subcarpeta visible.

        try {
            await api.put(`/Documentos/${docId}/mover`, { carpetaId: carpetaDestino.id });
            // Recargar para quitar el documento de la vista actual
            fetchContenido();
        } catch (error) {
            console.error('Error al mover documento:', error);
            alert('Error al mover el documento.');
        }
    };

    // --- Lógica de Carpetas ---
    const handleEnterCarpeta = (carpeta) => {
        setBreadcrumbs([...breadcrumbs, carpeta]);
        setCarpetaActual(carpeta);
        setBuscar(''); // Limpiar buscador al entrar para ver contenido
    };

    const handleNavigateBreadcrumb = (index) => {
        if (index === -1) {
            setBreadcrumbs([]);
            setCarpetaActual(null);
        } else {
            const newMigas = breadcrumbs.slice(0, index + 1);
            setBreadcrumbs(newMigas);
            setCarpetaActual(newMigas[newMigas.length - 1]);
        }
    };

    const handleSaveCarpeta = async (data) => {
        try {
            // Adjuntar ParentId si estamos dentro de una carpeta
            const payload = { ...data, parentId: carpetaActual?.id || null };
            await api.post('/CarpetasDocumentos', payload);
            fetchContenido();
        } catch (error) {
            console.error(error);
            alert('Error al crear carpeta');
        }
    };

    const handleDeleteCarpeta = async (e, id) => {
        e.stopPropagation();
        if (!confirm("¿Eliminar carpeta? Debe estar vacía.")) return;
        try {
            await api.delete(`/CarpetasDocumentos/${id}`);
            fetchContenido();
        } catch (error) {
            alert(error.response?.data || "Error al eliminar");
        }
    };

    // --- Lógica Documentos ---
    const handleSaveDocument = async (formData) => {
        // Adjuntar carpeta actual
        if (carpetaActual) {
            formData.append('carpetaId', carpetaActual.id);
        }
        await api.post('/Documentos', formData, {
            headers: { 'Content-Type': 'multipart/form-data' }
        });
        fetchContenido();
    };

    const handleDownload = async (docId, nombreArchivo) => {
        try {
            const response = await api.get(`/Documentos/${docId}/descargar`, {
                responseType: 'blob'
            });
            const url = window.URL.createObjectURL(new Blob([response.data]));
            const link = document.createElement('a');
            link.href = url;
            link.setAttribute('download', nombreArchivo || `documento_${docId}.pdf`);
            document.body.appendChild(link);
            link.click();
            link.remove();
        } catch (error) {
            alert('Error al descargar el archivo.');
        }
    };

    // Funciones de revisión (iguales)
    const handleRequestReview = async (docId) => {
        try { await api.post(`/Documentos/${docId}/solicitar-revision`); fetchContenido(); setActiveMenu(null); } catch (e) { alert(e.response?.data?.mensaje); }
    };
    const handleApprove = async (docId) => {
        try { await api.post(`/Documentos/${docId}/aprobar`); fetchContenido(); setActiveMenu(null); } catch (e) { alert(e.response?.data?.mensaje); }
    };
    const handleReject = async (docId) => {
        const obs = prompt('Observaciones:');
        if (!obs) return;
        const fd = new FormData(); fd.append('observaciones', obs);
        try { await api.post(`/Documentos/${docId}/rechazar`, fd); fetchContenido(); setActiveMenu(null); } catch (e) { alert(e.response?.data?.mensaje); }
    };

    const getEstadoIcon = (estado) => {
        switch (estado) {
            case 0: return <Clock className="status-icon pending" size={16} />;
            case 1: return <Clock className="status-icon review" size={16} />;
            case 2: return <CheckCircle2 className="status-icon approved" size={16} />;
            case 3: return <XCircle className="status-icon rejected" size={16} />;
            case 4: return <FileText className="status-icon obsolete" size={16} />;
            default: return null;
        }
    };

    // Helpers visuales
    const getAreaNombre = (area) => ['Dirección', 'Comercial', 'Operativa', 'Apoyo', 'Administrativa'][area] || 'GNR';
    const getEstadoNombre = (estado) => ['Borrador', 'En Revisión', 'Aprobado', 'Rechazado', 'Obsoleto'][estado] || 'Desc.';


    return (
        <div className="documentos-page">
            <header className="page-header">
                <div className="header-left">
                    {/* BREADCRUMBS */}
                    <div className="breadcrumbs">
                        <h1
                            onClick={() => handleNavigateBreadcrumb(-1)}
                            className={carpetaActual ? 'crumb-link' : 'crumb-active'}
                        >
                            Documentos
                        </h1>
                        {breadcrumbs.map((c, i) => (
                            <div key={c.id} style={{ display: 'flex', alignItems: 'center' }}>
                                <ChevronRight size={16} className="text-muted" />
                                <h1
                                    onClick={() => handleNavigateBreadcrumb(i)}
                                    className={i === breadcrumbs.length - 1 ? 'crumb-active' : 'crumb-link'}
                                >
                                    {c.nombre}
                                </h1>
                            </div>
                        ))}
                    </div>
                    <p>Control de información documentada NCh 2728</p>
                </div>
                {(user?.Rol === 'Administrador' || user?.Rol === 'Escritor') && (
                    <div style={{ display: 'flex', gap: '1rem' }}>
                        <button className="btn-secondary" onClick={() => setIsCarpetaModalOpen(true)}>
                            <FolderPlus size={20} />
                            <span>Nueva Carpeta</span>
                        </button>
                        <button className="btn-primary" onClick={() => setIsModalOpen(true)}>
                            <Plus size={20} />
                            <span>Nuevo Documento</span>
                        </button>
                    </div>
                )}
            </header>

            <section className="toolbar">
                <div className="search-box">
                    <Search size={20} className="search-icon" />
                    <input
                        type="text"
                        placeholder="Buscar por código o título..."
                        value={buscar}
                        onChange={(e) => setBuscar(e.target.value)}
                    />
                </div>
                {/* Filtros ... (Simplificados visualmente si se desea) */}
            </section>

            {/* --- ZONA DE CARPETAS --- */}
            {!buscar && (
                <div className="carpetas-grid" style={{ marginBottom: carpetas.length > 0 ? '2rem' : '0' }}>
                    {/* Botón para subir nivel si no estamos en raíz */}
                    {carpetaActual && (
                        <div
                            className="carpeta-card back-card"
                            onClick={() => handleNavigateBreadcrumb(breadcrumbs.length - 2)}
                        >
                            <ArrowLeft size={24} />
                            <span>Volver</span>
                        </div>
                    )}

                    {carpetas.map(carpeta => (
                        <div
                            key={carpeta.id}
                            className="carpeta-card"
                            onClick={() => handleEnterCarpeta(carpeta)}
                            onDragOver={handleDragOver}
                            onDrop={(e) => handleDrop(e, carpeta)}
                            style={{ borderColor: carpeta.color }}
                        >
                            <div className="carpeta-icon" style={{ color: carpeta.color }}>
                                <Folder size={32} fill={carpeta.color} fillOpacity={0.2} />
                            </div>
                            <span className="carpeta-name">{carpeta.nombre}</span>

                            {user?.Rol === 'Administrador' && (
                                <button className="btn-delete-folder" onClick={(e) => handleDeleteCarpeta(e, carpeta.id)}>
                                    <Trash2 size={14} />
                                </button>
                            )}
                        </div>
                    ))}
                </div>
            )}


            <div className="table-container card">
                {loading ? (
                    <div className="table-loading">Cargando...</div>
                ) : (
                    <table className="master-table">
                        <thead>
                            <tr>
                                <th>Código</th>
                                <th>Título</th>
                                <th>Área</th>
                                <th>Versión</th>
                                <th>Estado</th>
                                <th>Actualizado</th>
                                <th className="actions-col">Acciones</th>
                            </tr>
                        </thead>
                        <tbody>
                            {documentos.length > 0 ? documentos.map((doc) => (
                                <tr
                                    key={doc.id}
                                    draggable={user?.Rol === 'Administrador' || user?.Rol === 'Escritor'}
                                    onDragStart={(e) => handleDragStart(e, doc)}
                                    className="draggable-row"
                                >
                                    <td className="col-code"><strong>{doc.codigo}</strong></td>
                                    <td className="col-title">
                                        <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                                            <FileText size={16} className="text-muted" />
                                            {doc.titulo}
                                        </div>
                                    </td>
                                    <td className="col-area">
                                        <span className="area-tag">{getAreaNombre(doc.area)}</span>
                                    </td>
                                    <td className="col-version">v{doc.versionActual}.0</td>
                                    <td className="col-status">
                                        <div className={`status-pill state-${doc.estado}`}>
                                            {getEstadoIcon(doc.estado)}
                                            <span>{getEstadoNombre(doc.estado)}</span>
                                        </div>
                                    </td>
                                    <td className="col-date">
                                        {new Date(doc.fechaActualizacion || doc.fechaCreacion).toLocaleDateString()}
                                    </td>
                                    <td className="col-actions">
                                        <div className="action-buttons">
                                            <button
                                                className="action-btn"
                                                title="Descargar"
                                                onClick={() => {
                                                    const versionActual = doc.revisiones?.find(r => r.esVersionActual);
                                                    handleDownload(doc.id, versionActual?.nombreArchivo || `${doc.titulo}.pdf`);
                                                }}
                                            >
                                                <Download size={18} />
                                            </button>

                                            <div className="menu-container">
                                                <button
                                                    className={`action-btn ${activeMenu === doc.id ? 'active' : ''}`}
                                                    onClick={() => setActiveMenu(activeMenu === doc.id ? null : doc.id)}
                                                >
                                                    <MoreVertical size={18} />
                                                </button>

                                                {activeMenu === doc.id && (
                                                    <div className="dropdown-menu">
                                                        {doc.estado === 0 && (
                                                            <button onClick={() => handleRequestReview(doc.id)}>
                                                                <Send size={14} /> Solicitar Revisión
                                                            </button>
                                                        )}
                                                        {doc.estado === 1 && user?.Rol === 'Administrador' && (
                                                            <>
                                                                <button onClick={() => handleApprove(doc.id)} className="text-success">
                                                                    <CheckCircle size={14} /> Aprobar
                                                                </button>
                                                                <button onClick={() => handleReject(doc.id)} className="text-error">
                                                                    <XCircle size={14} /> Rechazar
                                                                </button>
                                                            </>
                                                        )}
                                                        <button className="text-muted">
                                                            <Eye size={14} /> Ver Historial
                                                        </button>
                                                    </div>
                                                )}
                                            </div>
                                        </div>
                                    </td>
                                </tr>
                            )) : (
                                <tr>
                                    <td colSpan="7" className="empty-row">
                                        {carpetaActual ? 'Carpeta vacía.' : 'No hay documentos en la raíz.'}
                                    </td>
                                </tr>
                            )}
                        </tbody>
                    </table>
                )}
            </div>

            <DocumentModal
                isOpen={isModalOpen}
                onClose={() => setIsModalOpen(false)}
                onSave={handleSaveDocument}
            />
            <CarpetaDocumentoModal
                isOpen={isCarpetaModalOpen}
                onClose={() => setIsCarpetaModalOpen(false)}
                onSave={handleSaveCarpeta}
            />
        </div>
    );
};

export default Documentos;
