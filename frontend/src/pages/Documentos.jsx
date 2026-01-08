import { useState, useEffect } from 'react';
import api from '../api/client';
import { useAuth } from '../context/AuthContext';
import { useNavigate } from 'react-router-dom';
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
    ChevronRight,
    Trash2,
    Move,
    LayoutGrid,
    Search as SearchIcon
} from 'lucide-react';

import DocumentModal from '../components/DocumentModal';
import CarpetaDocumentoModal from '../components/CarpetaDocumentoModal';
import RevisionModal from '../components/RevisionModal';
import MoveModal from '../components/MoveModal';
import FolderTree from '../components/FolderTree';
import SecureDocViewer from '../components/SecureDocViewer';
import '../styles/Documentos.css';

const Documentos = () => {
    const { user } = useAuth();
    const navigate = useNavigate();
    const [documentos, setDocumentos] = useState([]);
    const [loading, setLoading] = useState(true);
    const [buscar, setBuscar] = useState('');

    // Estados para Modales
    const [isModalOpen, setIsModalOpen] = useState(false);
    const [isCarpetaModalOpen, setIsCarpetaModalOpen] = useState(false);
    const [isRevisionModalOpen, setIsRevisionModalOpen] = useState(false);
    const [isMoveModalOpen, setIsMoveModalOpen] = useState(false);

    const [selectedDocForRevision, setSelectedDocForRevision] = useState(null);
    const [selectedDocForMove, setSelectedDocForMove] = useState(null);

    // Estado Visor Seguro
    const [viewerOpen, setViewerOpen] = useState(false);
    const [selectedDocDetails, setSelectedDocDetails] = useState(null);

    const [activeMenu, setActiveMenu] = useState(null);
    const [filtros, setFiltros] = useState({ area: '', tipo: '', estado: '' });

    // Navegación de Carpetas
    const [carpetaActual, setCarpetaActual] = useState(null);
    const [refreshTreeKey, setRefreshTreeKey] = useState(0);
    const [parentForNewFolder, setParentForNewFolder] = useState(null);

    const fetchContenido = async () => {
        setLoading(true);
        try {
            const params = {
                buscar: buscar || undefined,
                area: filtros.area || undefined,
                tipo: filtros.tipo || undefined,
                estado: filtros.estado || undefined,
                carpetaId: carpetaActual?.id || null
            };
            const res = await api.get('/Documentos', { params });
            setDocumentos(res.data);
        } catch (error) {
            console.error('Error al cargar contenido:', error);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        fetchContenido();
    }, [buscar, filtros, carpetaActual]);

    const handleSelectFolder = (carpeta) => {
        setCarpetaActual(carpeta);
    };

    const handleAddSubfolder = (parent) => {
        setParentForNewFolder(parent);
        setIsCarpetaModalOpen(true);
    };

    const handleDeleteFolder = async (carpeta) => {
        if (user?.Rol !== 'Administrador') {
            alert("Solo los administradores pueden eliminar carpetas.");
            return;
        }

        const confirm = window.confirm(`¿Estás seguro de eliminar la carpeta "${carpeta.nombre}"? Esta acción no se puede deshacer y fallará si la carpeta tiene contenido.`);
        if (!confirm) return;

        try {
            await api.delete(`/CarpetasDocumentos/${carpeta.id}`);
            setRefreshTreeKey(prev => prev + 1);
            if (carpetaActual?.id === carpeta.id) setCarpetaActual(null);
            fetchContenido();
        } catch (err) {
            console.error(err);
            alert("Error al eliminar carpeta: " + (err.response?.data || err.message));
        }
    };

    const handleSaveDocument = async (formData) => {
        if (carpetaActual) formData.append('carpetaId', carpetaActual.id);
        await api.post('/Documentos', formData, { headers: { 'Content-Type': 'multipart/form-data' } });
        fetchContenido();
    };

    const handleDownload = async (docId, nombreOriginal) => {
        try {
            const response = await api.get(`/Documentos/${docId}/descargar`, { responseType: 'blob' });
            const url = window.URL.createObjectURL(new Blob([response.data]));
            const link = document.createElement('a');
            link.href = url;
            link.setAttribute('download', nombreOriginal || `documento.pdf`);
            document.body.appendChild(link);
            link.click();
            link.remove();
        } catch (error) { alert('Error al descargar'); }
    };

    const handleView = (doc) => {
        const token = localStorage.getItem('token');
        setSelectedDocDetails({
            id: doc.id,
            url: { url: `${import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000/api'}/Documentos/${doc.id}/descargar`, httpHeaders: { 'Authorization': `Bearer ${token}` } },
            name: doc.titulo
        });
        setViewerOpen(true);
    };

    const getEstadoNombre = (estado) => ['Borrador', 'En Revisión', 'Aprobado', 'Rechazado', 'Obsoleto'][estado] || 'Desc.';
    const getAreaNombre = (area) => ['Dirección', 'Comercial', 'Operativa', 'Apoyo', 'Administrativa'][area] || 'GNR';

    return (
        <div className="documentos-page">
            <header className="page-header">
                <div className="header-left">
                    <h1>Gestión Documental SGC</h1>
                    <p>Explorador jerárquico de información documentada NCh 2728</p>
                </div>
                <div style={{ display: 'flex', gap: '12px' }}>
                    {user?.Rol === 'Administrador' && (
                        <>
                            <button className="btn-secondary" onClick={() => setIsCarpetaModalOpen(true)}>
                                <FolderPlus size={18} /> Nueva Carpeta
                            </button>
                            <button className="btn-primary" onClick={() => setIsModalOpen(true)}>
                                <Plus size={18} /> Subir Archivo
                            </button>
                        </>
                    )}
                    <button className="btn-primary" onClick={() => navigate('/redactar')}>
                        <FileText size={18} /> Redactar
                    </button>
                </div>
            </header>

            <div className="folders-layout">
                {/* PANEL IZQUIERDO: ÁRBOL (ESTILO FILEZILLA) */}
                <FolderTree
                    onSelect={handleSelectFolder}
                    selectedId={carpetaActual?.id || null}
                    refreshKey={refreshTreeKey}
                    onAddClick={handleAddSubfolder}
                    onDeleteClick={handleDeleteFolder}
                />

                {/* PANEL DERECHO: CONTENIDO */}
                <main className="main-content-explorer">
                    <div className="explorer-toolbar">
                        <div className="search-box" style={{ maxWidth: '300px' }}>
                            <SearchIcon size={16} className="search-icon" />
                            <input
                                type="text"
                                placeholder="Filtrar archivos..."
                                value={buscar}
                                onChange={(e) => setBuscar(e.target.value)}
                            />
                        </div>
                        <div className="current-folder-info" style={{ fontSize: '0.9rem', color: 'var(--text-secondary)' }}>
                            Ubicación: <strong>{carpetaActual ? carpetaActual.nombre : 'Raíz del Sistema'}</strong>
                        </div>
                    </div>

                    <div className="table-container">
                        {loading ? (
                            <div className="table-loading">Cargando archivos...</div>
                        ) : (
                            <table className="master-table">
                                <thead>
                                    <tr>
                                        <th>Código</th>
                                        <th>Título</th>
                                        <th>Área</th>
                                        <th>Versión</th>
                                        <th>Estado</th>
                                        <th>Acciones</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {documentos.length > 0 ? documentos.map(doc => (
                                        <tr key={doc.id}>
                                            <td style={{ color: 'var(--primary)', fontWeight: '600' }}>{doc.codigo}</td>
                                            <td>{doc.titulo}</td>
                                            <td><span className="area-tag">{getAreaNombre(doc.area)}</span></td>
                                            <td>v{doc.versionActual}.0</td>
                                            <td>
                                                <span className={`status-pill state-${doc.estado}`}>
                                                    {getEstadoNombre(doc.estado)}
                                                </span>
                                            </td>
                                            <td className="col-actions">
                                                <div className="action-buttons">
                                                    <button className="action-btn" onClick={() => handleView(doc)}><Eye size={18} /></button>
                                                    <button className="action-btn" onClick={() => handleDownload(doc.id, doc.nombreArchivoActual)}><Download size={18} /></button>
                                                    <div className="menu-container">
                                                        <button className="action-btn" onClick={() => setActiveMenu(activeMenu === doc.id ? null : doc.id)}><MoreVertical size={18} /></button>
                                                        {activeMenu === doc.id && (
                                                            <div className="dropdown-menu">
                                                                <button onClick={() => { setSelectedDocForMove(doc); setIsMoveModalOpen(true); setActiveMenu(null); }}><Move size={14} /> Mover</button>
                                                                <button onClick={() => navigate(`/redactar/${doc.id}`)}><FileText size={14} /> Editar</button>
                                                            </div>
                                                        )}
                                                    </div>
                                                </div>
                                            </td>
                                        </tr>
                                    )) : (
                                        <tr><td colSpan="6" className="empty-row">No hay documentos en esta carpeta.</td></tr>
                                    )}
                                </tbody>
                            </table>
                        )}
                    </div>
                </main>
            </div>

            <DocumentModal isOpen={isModalOpen} onClose={() => setIsModalOpen(false)} onSave={handleSaveDocument} nombreCarpeta={carpetaActual?.nombre} />
            <CarpetaDocumentoModal
                isOpen={isCarpetaModalOpen}
                onClose={() => { setIsCarpetaModalOpen(false); setParentForNewFolder(null); }}
                nombreCarpetaPadre={parentForNewFolder?.nombre || carpetaActual?.nombre}
                onSave={async (d) => {
                    try {
                        await api.post('/CarpetasDocumentos', {
                            ...d,
                            ParentId: parentForNewFolder?.id || carpetaActual?.id || null
                        });
                        setRefreshTreeKey(prev => prev + 1);
                        fetchContenido();
                    } catch (err) {
                        console.error(err);
                        alert("Error al crear la carpeta: " + (err.response?.data || err.message));
                    }
                }}
            />
            {selectedDocForRevision && <RevisionModal isOpen={isRevisionModalOpen} onClose={() => setIsRevisionModalOpen(false)} docId={selectedDocForRevision.id} docTitulo={selectedDocForRevision.titulo} onSave={fetchContenido} />}
            {selectedDocForMove && <MoveModal isOpen={isMoveModalOpen} onClose={() => setIsMoveModalOpen(false)} docId={selectedDocForMove.id} docTitulo={selectedDocForMove.titulo} onSave={fetchContenido} />}
            {viewerOpen && selectedDocDetails && <SecureDocViewer fileUrl={selectedDocDetails.url} fileName={selectedDocDetails.name} docId={selectedDocDetails.id} onClose={() => setViewerOpen(false)} user={user} />}
        </div>
    );
};

export default Documentos;
