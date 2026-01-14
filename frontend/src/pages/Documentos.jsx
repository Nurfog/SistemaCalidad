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
    Search as SearchIcon,
    CloudUpload,
    Cpu,
    Bot,
    Sparkles,
    MessageSquare,
    Info
} from 'lucide-react';

import DocumentModal from '../components/DocumentModal';
import CarpetaDocumentoModal from '../components/CarpetaDocumentoModal';
import RevisionModal from '../components/RevisionModal';
import MoveModal from '../components/MoveModal';
import RenameModal from '../components/RenameModal';
import FolderTree from '../components/FolderTree';
import SecureDocViewer from '../components/SecureDocViewer';
import BulkUploadDialog from '../components/BulkUpload/BulkUploadDialog'; // Nuevo componente
import '../styles/Documentos.css';

const Documentos = () => {
    const { user } = useAuth();
    const navigate = useNavigate();
    const [documentos, setDocumentos] = useState([]);
    const [subcarpetas, setSubcarpetas] = useState([]);
    const [loading, setLoading] = useState(true);
    const [buscar, setBuscar] = useState('');

    // Estados para Modales
    const [isModalOpen, setIsModalOpen] = useState(false);
    const [isCarpetaModalOpen, setIsCarpetaModalOpen] = useState(false);
    const [isRevisionModalOpen, setIsRevisionModalOpen] = useState(false);
    const [isMoveModalOpen, setIsMoveModalOpen] = useState(false);
    const [isRenameModalOpen, setIsRenameModalOpen] = useState(false);
    const [isBulkUploadOpen, setIsBulkUploadOpen] = useState(false); // Estado para carga masiva

    const [selectedDocForRevision, setSelectedDocForRevision] = useState(null);
    const [selectedDocForMove, setSelectedDocForMove] = useState(null);
    const [selectedDocForRename, setSelectedDocForRename] = useState(null);

    // Estado Visor Seguro
    const [viewerOpen, setViewerOpen] = useState(false);
    const [selectedDocDetails, setSelectedDocDetails] = useState(null);

    const [activeMenu, setActiveMenu] = useState(null);
    const [filtros, setFiltros] = useState({ area: '', tipo: '', estado: '' });

    // ESTADOS PARA SAMITO (IA)
    const [samitoLoading, setSamitoLoading] = useState(false);
    const [samitoResult, setSamitoResult] = useState(null);
    const [isSamitoMode, setIsSamitoMode] = useState(false);
    const [samitoConfig, setSamitoConfig] = useState({ nombre: 'Samito', dominio: 'SGC' });

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

            // Fetch Documents
            const resDocs = await api.get('/Documentos', { params });
            setDocumentos(resDocs.data);

            // Fetch Subfolders (only if not searching, to maintain standard explorer behavior)
            if (!buscar) {
                const resFolders = await api.get('/CarpetasDocumentos', {
                    params: { parentId: carpetaActual?.id || null }
                });
                setSubcarpetas(resFolders.data);
            } else {
                setSubcarpetas([]);
            }

            // Si hay búsqueda semántica activa, filtramos los documentos
            if (isSamitoMode && samitoResult?.codigosArchivos?.length > 0) {
                const filtrados = resDocs.data.filter(d =>
                    samitoResult.codigosArchivos.some(code =>
                        d.codigo.toLowerCase().includes(code.toLowerCase())
                    )
                );
                setDocumentos(filtrados);
            }
        } catch (error) {
            console.error('Error al cargar contenido:', error);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        fetchContenido();

    }, [carpetaActual, filtros]);

    const handleSearchSubmit = async (e) => {
        e.preventDefault();
        if (!buscar) {
            setIsSamitoMode(false);
            setSamitoResult(null);
            fetchContenido();
            return;
        }

        if (isSamitoMode) {
            setSamitoLoading(true);
            setSamitoResult(null);
            try {
                const res = await api.get('/IA/buscar-semantica', { params: { query: buscar } });
                setSamitoResult(res.data);

                // Actualizar documentos basados en códigos de Samito
                const resAllDocs = await api.get('/Documentos'); // Traemos todos para buscar semánticamente
                const filtrados = resAllDocs.data.filter(d =>
                    res.data.codigosArchivos.some(code =>
                        d.codigo.toLowerCase().includes(code.toLowerCase())
                    )
                );
                setDocumentos(filtrados);
                setSubcarpetas([]);
            } catch (error) {
                console.error('Error en búsqueda de Samito:', error);
                alert('Samito tuvo un problema. Se realizará una búsqueda normal.');
                fetchContenido();
            } finally {
                setSamitoLoading(false);
            }
        } else {
            fetchContenido();
        }
    };

    const handleSelectFolder = (carpeta) => {
        setCarpetaActual(carpeta);
        setBuscar(''); // Limpiar búsqueda al navegar entre carpetas
    };

    const handleGoUp = async () => {
        if (!carpetaActual || !carpetaActual.parentId) {
            setCarpetaActual(null);
            return;
        }

        try {
            // Necesitamos los datos de la carpeta padre para el estado carpetaActual
            // Podríamos implementar un endpoint GetById en el backend si no lo hay
            const res = await api.get(`/CarpetasDocumentos/Arbol`); // O usar el árbol ya cargado
            const parent = findInArbol(res.data, carpetaActual.parentId);
            setCarpetaActual(parent);
        } catch (e) {
            setCarpetaActual(null); // Fallback a raíz
        }
    };

    const findInArbol = (nodes, id) => {
        for (const node of nodes) {
            if (node.id === id) return node;
            if (node.subCarpetas) {
                const found = findInArbol(node.subCarpetas, id);
                if (found) return found;
            }
        }
        return null;
    };

    const handleAddSubfolder = (parent) => {
        setParentForNewFolder(parent);
        setIsCarpetaModalOpen(true);
    };

    const handleDeleteFolder = async (carpeta) => {
        if (!user?.Rol?.includes('Administrador')) {
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
        if (carpetaActual) formData.append('carpetaIds', carpetaActual.id);
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

    const handleSyncRAG = async () => {
        if (!window.confirm("¿Seguro que deseas re-entrenar la Inteligencia Artificial con todos los documentos actuales? Esto puede tardar unos minutos.")) return;

        setSamitoLoading(true);
        setIsSamitoMode(true);
        setSamitoResult({ mensaje: "Iniciando proceso de aprendizaje..." });

        try {
            const res = await api.post('/Documentos/rag-sync-total');
            setSamitoResult({
                mensaje: `¡Aprendizaje completado!\nDocumentos procesados: ${res.data.documentosProcesados}`
            });
        } catch (error) {
            console.error("Error sync RAG:", error);
            setSamitoResult({ mensaje: "Hubo un error al intentar aprender de los documentos. Revisa la consola." });
        } finally {
            setSamitoLoading(false);
        }
    };

    return (
        <div className="documentos-page">
            <header className="page-header">
                <div className="header-left">
                    <h1>Gestión Documental SGC</h1>
                    <p>Explorador jerárquico de información documentada NCh 2728</p>
                </div>
                <div style={{ display: 'flex', gap: '12px' }}>
                    {user?.Rol?.includes('Administrador') && (
                        <>
                            <button className="btn-secondary" onClick={handleSyncRAG} title="Re-entrenar IA con documentos actuales">
                                <Bot size={18} /> Entrenar IA
                            </button>
                            <button className="btn-secondary" onClick={() => setIsCarpetaModalOpen(true)}>
                                <FolderPlus size={18} /> Nueva Carpeta
                            </button>
                            <button className="btn-primary" onClick={() => setIsModalOpen(true)}>
                                <Plus size={18} /> Subir Archivo
                            </button>
                            <button className="btn-secondary" style={{ backgroundColor: '#4f46e5', color: 'white' }} onClick={() => setIsBulkUploadOpen(true)}>
                                <CloudUpload size={18} /> Carga Masiva
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
                    <div className="explorer-toolbar" style={{ display: 'flex', flexDirection: 'column', gap: '16px', marginBottom: '20px' }}>
                        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', width: '100%', gap: '12px' }}>
                            <form
                                onSubmit={handleSearchSubmit}
                                className={`search-box ${isSamitoMode ? 'samito-active' : ''}`}
                                style={{ maxWidth: '450px', flex: 1, display: 'flex', alignItems: 'center' }}
                            >
                                <SearchIcon size={16} className="search-icon" />
                                <input
                                    type="text"
                                    placeholder={isSamitoMode ? `Pregúntele a ${samitoConfig.nombre}...` : "Filtrar archivos..."}
                                    value={buscar}
                                    onChange={(e) => setBuscar(e.target.value)}
                                    style={{ flex: 1 }}
                                />
                                <button
                                    type="button"
                                    className={`samito-toggle-btn ${isSamitoMode ? 'active' : ''}`}
                                    onClick={() => setIsSamitoMode(!isSamitoMode)}
                                    title={isSamitoMode ? `Desactivar ${samitoConfig.nombre}` : `Activar ${samitoConfig.nombre} (IA)`}
                                >
                                    <Sparkles size={14} className={isSamitoMode ? 'sparkle-spin' : ''} />
                                    <span>{samitoConfig.nombre.split(' ')[0]}</span>
                                </button>
                            </form>
                            <div className="current-folder-info" style={{ fontSize: '0.9rem', color: 'var(--text-secondary)', whiteSpace: 'nowrap' }}>
                                Ubicación: <strong>{carpetaActual ? carpetaActual.nombre : 'Raíz del Sistema'}</strong>
                            </div>
                        </div>

                        {/* PANEL DE SAMITO */}
                        {(samitoLoading || samitoResult) && (
                            <div className={`samito-response-card ${samitoLoading ? 'loading' : ''}`}>
                                <div className="samito-card-header">
                                    <div className="samito-branding">
                                        <div className="samito-avatar">
                                            <Bot size={18} />
                                        </div>
                                        <div className="samito-id">
                                            <span className="samito-name">{samitoConfig.nombre}</span>
                                            <span className="samito-label">Asistente Inteligente</span>
                                        </div>
                                    </div>
                                    {samitoResult && <button className="samito-panel-close" onClick={() => setSamitoResult(null)}>×</button>}
                                </div>
                                <div className="samito-card-body">
                                    {samitoLoading ? (
                                        <div className="samito-loading-state">
                                            <div className="loading-pulse"></div>
                                            <span className="loading-text">Analizando base de conocimientos...</span>
                                        </div>
                                    ) : (
                                        <div className="samito-content">
                                            <p className="samito-message">{samitoResult.resena}</p>
                                            <div className="samito-tip">
                                                <Info size={14} />
                                                <span>He filtrado la lista para mostrarte los documentos específicos mencionados.</span>
                                            </div>
                                        </div>
                                    )}
                                </div>
                            </div>
                        )}
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
                                    {/* Carpeta Superior (Ir Arriba) */}
                                    {carpetaActual && !buscar && (
                                        <tr className="folder-row up-row" onClick={handleGoUp} style={{ cursor: 'pointer', background: 'rgba(56, 189, 248, 0.03)' }}>
                                            <td colSpan="6">
                                                <div style={{ display: 'flex', alignItems: 'center', gap: '12px', padding: '4px 0' }}>
                                                    <div style={{ width: '32px', display: 'flex', justifyContent: 'center' }}>
                                                        <Move size={16} style={{ transform: 'rotate(-90deg)', color: 'var(--text-secondary)' }} />
                                                    </div>
                                                    <span style={{ fontWeight: '600', color: 'var(--text-secondary)' }}>... (Carpeta Superior)</span>
                                                </div>
                                            </td>
                                        </tr>
                                    )}

                                    {/* Subcarpetas */}
                                    {subcarpetas.map(folder => (
                                        <tr key={`folder-${folder.id}`} className="folder-row" onClick={() => handleSelectFolder(folder)} style={{ cursor: 'pointer' }}>
                                            <td colSpan="2">
                                                <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
                                                    <div className="folder-icon-wrapper" style={{
                                                        backgroundColor: `${folder.color || '#3b82f6'}15`,
                                                        color: folder.color || '#3b82f6',
                                                        padding: '6px',
                                                        borderRadius: '8px',
                                                        display: 'flex'
                                                    }}>
                                                        <FolderPlus size={18} fill={folder.color || '#3b82f6'} fillOpacity={0.2} />
                                                    </div>
                                                    <span style={{ fontWeight: '600' }}>{folder.nombre}</span>
                                                </div>
                                            </td>
                                            <td><span className="area-tag" style={{ background: 'var(--bg-secondary)', color: 'var(--text-secondary)' }}>Carpeta</span></td>
                                            <td>--</td>
                                            <td>--</td>
                                            <td className="col-actions">
                                                <button
                                                    className="action-btn"
                                                    onClick={(e) => { e.stopPropagation(); handleDeleteFolder(folder); }}
                                                    style={{ color: 'var(--error)' }}
                                                    title="Eliminar"
                                                >
                                                    <Trash2 size={18} />
                                                </button>
                                            </td>
                                        </tr>
                                    ))}

                                    {/* Documentos */}
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
                                                                {(user?.Rol?.includes('Administrador') || user?.Rol?.includes('Responsable')) && (
                                                                    <button onClick={() => { setSelectedDocForRename(doc); setIsRenameModalOpen(true); setActiveMenu(null); }}><FileText size={14} /> Renombrar</button>
                                                                )}
                                                                <button onClick={() => navigate(`/redactar/${doc.id}`)}><FileText size={14} /> Editar</button>
                                                            </div>

                                                        )}
                                                    </div>
                                                </div>
                                            </td>
                                        </tr>
                                    )) : (
                                        (!subcarpetas.length && !carpetaActual) && <tr><td colSpan="6" className="empty-row">No hay contenido en esta ubicación.</td></tr>
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
            {isBulkUploadOpen && (
                <BulkUploadDialog
                    open={isBulkUploadOpen}
                    onClose={() => setIsBulkUploadOpen(false)}
                    currentFolderId={carpetaActual?.id || null}
                    onUploadComplete={() => {
                        setRefreshTreeKey(prev => prev + 1); // Refresh tree for new folders
                        fetchContenido(); // Refresh files
                    }}
                />
            )}
            {selectedDocForRevision && <RevisionModal isOpen={isRevisionModalOpen} onClose={() => setIsRevisionModalOpen(false)} docId={selectedDocForRevision.id} docTitulo={selectedDocForRevision.titulo} onSave={fetchContenido} />}
            {selectedDocForMove && <MoveModal isOpen={isMoveModalOpen} onClose={() => setIsMoveModalOpen(false)} docId={selectedDocForMove.id} docTitulo={selectedDocForMove.titulo} onSave={fetchContenido} />}
            {selectedDocForRename && <RenameModal isOpen={isRenameModalOpen} onClose={() => setIsRenameModalOpen(false)} docId={selectedDocForRename.id} currentTitulo={selectedDocForRename.titulo} currentCodigo={selectedDocForRename.codigo} onSave={fetchContenido} />}
            {viewerOpen && selectedDocDetails && <SecureDocViewer fileUrl={selectedDocDetails.url} fileName={selectedDocDetails.name} docId={selectedDocDetails.id} onClose={() => setViewerOpen(false)} user={user} />}
        </div>
    );
};

export default Documentos;
