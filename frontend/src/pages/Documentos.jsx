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
    CheckCircle
} from 'lucide-react';
import DocumentModal from '../components/DocumentModal';
import '../styles/Documentos.css';

const Documentos = () => {
    const { user } = useAuth();
    const [documentos, setDocumentos] = useState([]);
    const [loading, setLoading] = useState(true);
    const [buscar, setBuscar] = useState('');
    const [isModalOpen, setIsModalOpen] = useState(false);
    const [activeMenu, setActiveMenu] = useState(null);
    const [filtros, setFiltros] = useState({
        area: '',
        tipo: '',
        estado: ''
    });

    const fetchDocumentos = async () => {
        setLoading(true);
        try {
            const params = {
                buscar,
                area: filtros.area || undefined,
                tipo: filtros.tipo || undefined,
                estado: filtros.estado || undefined
            };
            const response = await api.get('/Documentos', { params });
            setDocumentos(response.data);
        } catch (error) {
            console.error('Error al cargar documentos:', error);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        const timer = setTimeout(() => {
            fetchDocumentos();
        }, 500);
        return () => clearTimeout(timer);
    }, [buscar, filtros]);

    const handleSaveDocument = async (formData) => {
        await api.post('/Documentos', formData, {
            headers: { 'Content-Type': 'multipart/form-data' }
        });
        fetchDocumentos();
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
            alert('Error al descargar el archivo. Es posible que no tengas permisos.');
        }
    };

    const handleRequestReview = async (docId) => {
        try {
            await api.post(`/Documentos/${docId}/solicitar-revision`);
            fetchDocumentos();
            setActiveMenu(null);
        } catch (error) {
            alert(error.response?.data?.mensaje || 'Error al solicitar revisión');
        }
    };

    const handleApprove = async (docId) => {
        try {
            await api.post(`/Documentos/${docId}/aprobar`);
            fetchDocumentos();
            setActiveMenu(null);
        } catch (error) {
            alert(error.response?.data?.mensaje || 'Error al aprobar');
        }
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

    const getEstadoNombre = (estado) => {
        const nombres = ['Borrador', 'En Revisión', 'Aprobado', 'Rechazado', 'Obsoleto'];
        return nombres[estado] || 'Desconocido';
    };

    const getAreaNombre = (area) => {
        const nombres = ['Dirección', 'Comercial', 'Operativa', 'Apoyo', 'Administrativa'];
        return nombres[area] || 'GNR';
    };

    return (
        <div className="documentos-page">
            <header className="page-header">
                <div className="header-left">
                    <h1>Listado Maestro de Documentos</h1>
                    <p>Control de información documentada NCh 2728</p>
                </div>
                {(user?.Rol === 'Administrador' || user?.Rol === 'Escritor') && (
                    <button className="btn-primary" onClick={() => setIsModalOpen(true)}>
                        <Plus size={20} />
                        <span>Nuevo Documento</span>
                    </button>
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

                <div className="filter-group">
                    <select
                        value={filtros.area}
                        onChange={(e) => setFiltros({ ...filtros, area: e.target.value })}
                    >
                        <option value="">Todas las Áreas</option>
                        <option value="0">Dirección</option>
                        <option value="1">Comercial</option>
                        <option value="2">Operativa</option>
                        <option value="3">Apoyo</option>
                        <option value="4">Administrativa</option>
                    </select>

                    <select
                        value={filtros.estado}
                        onChange={(e) => setFiltros({ ...filtros, estado: e.target.value })}
                    >
                        <option value="">Todos los Estados</option>
                        <option value="0">Borrador</option>
                        <option value="1">En Revisión</option>
                        <option value="2">Aprobado</option>
                    </select>
                </div>
            </section>

            <div className="table-container card">
                {loading ? (
                    <div className="table-loading">Buscando documentos...</div>
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
                                <tr key={doc.id}>
                                    <td className="col-code"><strong>{doc.codigo}</strong></td>
                                    <td className="col-title">{doc.titulo}</td>
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
                                                onClick={() => handleDownload(doc.id, doc.titulo + '.pdf')}
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
                                                        {doc.estado === 0 && (user?.Rol === 'Administrador' || user?.Rol === 'Escritor') && (
                                                            <button onClick={() => handleRequestReview(doc.id)}>
                                                                <Send size={14} /> Solicitar Revisión
                                                            </button>
                                                        )}
                                                        {doc.estado === 1 && user?.Rol === 'Administrador' && (
                                                            <button onClick={() => handleApprove(doc.id)} className="text-success">
                                                                <CheckCircle size={14} /> Aprobar Documento
                                                            </button>
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
                                    <td colSpan="7" className="empty-row">No se encontraron documentos.</td>
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
        </div>
    );
};

export default Documentos;
