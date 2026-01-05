import { useState, useEffect } from 'react';
import api from '../api/client';
import {
    Search,
    Filter,
    Download,
    Eye,
    Plus,
    FileText,
    Clock,
    CheckCircle2,
    XCircle,
    MoreVertical
} from 'lucide-react';
import '../styles/Documentos.css';

const Documentos = () => {
    const [documentos, setDocumentos] = useState([]);
    const [loading, setLoading] = useState(true);
    const [buscar, setBuscar] = useState('');
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
        }, 500); // Debounce de búsqueda
        return () => clearTimeout(timer);
    }, [buscar, filtros]);

    const getEstadoIcon = (estado) => {
        switch (estado) {
            case 0: return <Clock className="status-icon pending" size={16} />; // Borrador
            case 1: return <Clock className="status-icon review" size={16} />; // En Revision
            case 2: return <CheckCircle2 className="status-icon approved" size={16} />; // Aprobado
            case 3: return <XCircle className="status-icon rejected" size={16} />; // Rechazado
            case 4: return <FileText className="status-icon obsolete" size={16} />; // Obsoleto
            default: return null;
        }
    };

    const getEstadoNombre = (estado) => {
        const nombres = ['Borrador', 'En Revisión', 'Aprobado', 'Rechazado', 'Obsoleto'];
        return nombres[estado] || 'Desconocido';
    };

    return (
        <div className="documentos-page">
            <header className="page-header">
                <div className="header-left">
                    <h1>Listado Maestro de Documentos</h1>
                    <p>Control de información documentada NCh 2728</p>
                </div>
                <button className="btn-primary">
                    <Plus size={20} />
                    <span>Nuevo Documento</span>
                </button>
            </header>

            {/* Barra de Herramientas */}
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
                        <option value="1">Gestión Calidad</option>
                        <option value="2">Operaciones</option>
                        <option value="3">Ventas</option>
                        <option value="4">Recursos Humanos</option>
                    </select>

                    <select
                        value={filtros.estado}
                        onChange={(e) => setFiltros({ ...filtros, estado: e.target.value })}
                    >
                        <option value="">Todos los Estados</option>
                        <option value="0">Borrador</option>
                        <option value="2">Aprobado</option>
                    </select>
                </div>
            </section>

            {/* Tabla de Documentos */}
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
                                        <span className="area-tag">{doc.areaNombre || 'GNR'}</span>
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
                                            <button className="action-btn" title="Ver Detalles"><Eye size={18} /></button>
                                            <button className="action-btn" title="Descargar"><Download size={18} /></button>
                                            <button className="action-btn"><MoreVertical size={18} /></button>
                                        </div>
                                    </td>
                                </tr>
                            )) : (
                                <tr>
                                    <td colSpan="7" className="empty-row">No se encontraron documentos que coincidan con los filtros.</td>
                                </tr>
                            )}
                        </tbody>
                    </table>
                )}
            </div>
        </div>
    );
};

export default Documentos;
