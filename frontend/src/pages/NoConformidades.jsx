import { useState, useEffect } from 'react';
import api from '../api/client';
import { useAuth } from '../context/AuthContext';
import {
    Search,
    Plus,
    AlertCircle,
    Clock,
    CheckCircle2,
    ShieldAlert,
    ChevronDown,
    Calendar,
    User,
    ArrowRight
} from 'lucide-react';
import NCModal from '../components/NCModal';
import NCDetailsModal from '../components/NCDetailsModal';
import '../styles/NoConformidades.css';

const NoConformidades = () => {
    const { user } = useAuth();
    const [noConformidades, setNoConformidades] = useState([]);
    const [loading, setLoading] = useState(true);
    const [buscar, setBuscar] = useState('');
    const [isModalOpen, setIsModalOpen] = useState(false);
    const [selectedNC, setSelectedNC] = useState(null);
    const [isDetailsOpen, setIsDetailsOpen] = useState(false);
    const [filtros, setFiltros] = useState({
        origen: '',
        estado: ''
    });

    const fetchNC = async () => {
        setLoading(true);
        try {
            const response = await api.get('/NoConformidades');
            setNoConformidades(response.data);
        } catch (error) {
            console.error('Error al cargar No Conformidades:', error);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        fetchNC();
    }, []);

    const handleSaveNC = async (data) => {
        try {
            await api.post('/NoConformidades', data);
            fetchNC();
        } catch (error) {
            console.error('Error al guardar NC:', error);
            throw error;
        }
    };

    const handleViewDetails = (nc) => {
        setSelectedNC(nc);
        setIsDetailsOpen(true);
    };

    const handleNCUpdate = async () => {
        const response = await api.get('/NoConformidades');
        setNoConformidades(response.data);

        // Mantener el modal actualizado con la data fresca del servidor
        if (selectedNC) {
            const updated = response.data.find(n => n.id === selectedNC.id);
            if (updated) setSelectedNC(updated);
        }
    };

    const ncFiltradas = noConformidades.filter(nc => {
        const matchBuscar = (nc.folio || "").toLowerCase().includes(buscar.toLowerCase()) ||
            (nc.descripcionHallazgo || "").toLowerCase().includes(buscar.toLowerCase());
        const matchOrigen = filtros.origen === "" || nc.origen.toString() === filtros.origen;
        const matchEstado = filtros.estado === "" || nc.estado.toString() === filtros.estado;

        return matchBuscar && matchOrigen && matchEstado;
    });

    const getEstadoIcon = (estado) => {
        switch (estado) {
            case 0: return <AlertCircle className="nc-status-icon open" size={16} />;
            case 1: return <Clock className="nc-status-icon analysis" size={16} />;
            case 2: return <Clock className="nc-status-icon implementation" size={16} />;
            case 3: return <CheckCircle2 className="nc-status-icon verified" size={16} />;
            case 4: return <CheckCircle2 className="nc-status-icon closed" size={16} />;
            default: return null;
        }
    };

    const getEstadoNombre = (estado) => {
        const nombres = ['Abierta', 'En Análisis', 'En Implementación', 'Verificada', 'Cerrada'];
        return nombres[estado] || 'Desconocido';
    };

    const getOrigenNombre = (origen) => {
        const nombres = [
            'Auditoría Interna',
            'Auditoría Externa',
            'Reclamo Cliente',
            'Revisión Dirección',
            'Incumplimiento Proceso',
            'Sugerencia Mejora'
        ];
        return nombres[origen] || 'Otros';
    };

    return (
        <div className="nc-page">
            <header className="page-header">
                <div className="header-left">
                    <h1>No Conformidades y Acciones</h1>
                    <p>Gestión de hallazgos y mejora continua NCh 2728</p>
                </div>
                {(user?.Rol?.includes('Administrador') || user?.Rol?.includes('Escritor')) && (
                    <button className="btn-primary" onClick={() => setIsModalOpen(true)}>
                        <Plus size={20} />
                        <span>Levantar NC</span>
                    </button>
                )}
            </header>

            <section className="nc-stats card">
                <div className="stat-item">
                    <span className="stat-value">{noConformidades.length}</span>
                    <span className="stat-label">Total Hallazgos</span>
                </div>
                <div className="stat-divider"></div>
                <div className="stat-item">
                    <span className="stat-value text-error">
                        {noConformidades.filter(nc => nc.estado === 0).length}
                    </span>
                    <span className="stat-label">Abiertas</span>
                </div>
                <div className="stat-divider"></div>
                <div className="stat-item">
                    <span className="stat-value text-success">
                        {noConformidades.filter(nc => nc.estado === 4).length}
                    </span>
                    <span className="stat-label">Cerradas</span>
                </div>
            </section>

            <section className="toolbar">
                <div className="search-box">
                    <Search size={20} className="search-icon" />
                    <input
                        type="text"
                        placeholder="Buscar por folio o descripción..."
                        value={buscar}
                        onChange={(e) => setBuscar(e.target.value)}
                    />
                </div>

                <div className="filter-group">
                    <select
                        value={filtros.origen}
                        onChange={(e) => setFiltros({ ...filtros, origen: e.target.value })}
                    >
                        <option value="">Todos los Orígenes</option>
                        <option value="0">Auditoría Interna</option>
                        <option value="1">Auditoría Externa</option>
                        <option value="2">Reclamo Cliente</option>
                        <option value="4">Incumplimiento Proceso</option>
                    </select>

                    <select
                        value={filtros.estado}
                        onChange={(e) => setFiltros({ ...filtros, estado: e.target.value })}
                    >
                        <option value="">Todos los Estados</option>
                        <option value="0">Abierta</option>
                        <option value="1">En Análisis</option>
                        <option value="4">Cerrada</option>
                    </select>
                </div>
            </section>

            <div className="nc-grid">
                {loading ? (
                    <div className="loading-state">Cargando hallazgos...</div>
                ) : (
                    ncFiltradas.map((nc) => (
                        <div key={nc.id} className="nc-card card">
                            <div className="nc-card-header">
                                <span className="nc-folio">{nc.folio}</span>
                                <div className={`nc-status state-${nc.estado}`}>
                                    {getEstadoIcon(nc.estado)}
                                    <span>{getEstadoNombre(nc.estado)}</span>
                                </div>
                            </div>

                            <div className="nc-card-body">
                                <h3 className="nc-origin">{getOrigenNombre(nc.origen)}</h3>
                                <p className="nc-description">{nc.descripcionHallazgo}</p>

                                <div className="nc-meta">
                                    <div className="meta-item">
                                        <Calendar size={14} />
                                        <span>{new Date(nc.fechaDeteccion).toLocaleDateString()}</span>
                                    </div>
                                    <div className="meta-item">
                                        <User size={14} />
                                        <span>{nc.detectadoPor}</span>
                                    </div>
                                </div>
                            </div>

                            <div className="nc-card-footer">
                                <div className="nc-actions-count">
                                    <ShieldAlert size={16} />
                                    <span>{nc.acciones?.length || 0} Acciones</span>
                                </div>
                                <button className="btn-text" onClick={() => handleViewDetails(nc)}>
                                    <span>Ver Detalles</span>
                                    <ArrowRight size={16} />
                                </button>
                            </div>
                        </div>
                    ))
                )}
                {noConformidades.length === 0 && !loading && (
                    <div className="empty-state card">
                        <AlertCircle size={40} />
                        <p>No se han registrado No Conformidades aún.</p>
                    </div>
                )}
            </div>

            <NCModal
                isOpen={isModalOpen}
                onClose={() => setIsModalOpen(false)}
                onSave={handleSaveNC}
            />

            <NCDetailsModal
                isOpen={isDetailsOpen}
                onClose={() => setIsDetailsOpen(false)}
                nc={selectedNC}
                onUpdate={handleNCUpdate}
            />
        </div>
    );
};

export default NoConformidades;
