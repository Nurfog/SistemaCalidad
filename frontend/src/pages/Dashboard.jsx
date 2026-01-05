import { useState, useEffect } from 'react';
import api from '../api/client';
import {
    FileText,
    AlertCircle,
    CheckCircle,
    Clock,
    ExternalLink,
    ChevronRight
} from 'lucide-react';
import '../styles/Dashboard.css';

const Dashboard = () => {
    const [stats, setStats] = useState(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        const fetchStats = async () => {
            try {
                const response = await api.get('/Dashboard/stats');
                setStats(response.data);
            } catch (error) {
                console.error('Error al cargar estadísticas:', error);
            } finally {
                setLoading(false);
            }
        };

        fetchStats();
    }, []);

    if (loading) return <div className="loading">Cargando estadísticas...</div>;

    return (
        <div className="dashboard-page">
            <header className="dashboard-header">
                <h1>Dashboard Normativo</h1>
                <p>Resumen del Sistema de Gestión de Calidad (NCh 2728)</p>
            </header>

            {/* Tarjetas Principales */}
            <div className="stats-grid">
                <div className="stat-card">
                    <div className="stat-icon docs"><FileText size={24} /></div>
                    <div className="stat-info">
                        <span className="stat-label">Total Documentos</span>
                        <span className="stat-value">{stats?.totalDocumentos || 0}</span>
                    </div>
                </div>

                <div className="stat-card warning">
                    <div className="stat-icon alerts"><AlertCircle size={24} /></div>
                    <div className="stat-info">
                        <span className="stat-label">Vencidos o Alerta</span>
                        <span className="stat-value">{stats?.alertasCriticas?.length || 0}</span>
                    </div>
                    {stats?.alertasCriticas?.length > 0 && <span className="card-badge">Crítico</span>}
                </div>

                <div className="stat-card success">
                    <div className="stat-icon check"><CheckCircle size={24} /></div>
                    <div className="stat-info">
                        <span className="stat-label">No Conformidades Abiertas</span>
                        <span className="stat-value">{stats?.noConformidadesAbiertas || 0}</span>
                    </div>
                </div>

                <div className="stat-card info">
                    <div className="stat-icon clock"><Clock size={24} /></div>
                    <div className="stat-info">
                        <span className="stat-label">En Revisión</span>
                        <span className="stat-value">
                            {stats?.documentosPorEstado?.find(e => e.estado === 'EnRevision')?.cantidad || 0}
                        </span>
                    </div>
                </div>
            </div>

            <div className="dashboard-main-content">
                {/* Alertas Críticas */}
                <div className="content-card alerts-section">
                    <div className="card-header">
                        <h3>Alertas de Revisión Anual</h3>
                        <button className="view-all">Ver todos <ChevronRight size={16} /></button>
                    </div>
                    <div className="alerts-list">
                        {stats?.alertasCriticas?.length > 0 ? (
                            stats.alertasCriticas.map((alerta, idx) => (
                                <div key={idx} className="alert-item">
                                    <div className="alert-text">
                                        <strong>{alerta.nombre}</strong>
                                        <span>Última revisión: {new Date(alerta.ultimaRevision).toLocaleDateString()}</span>
                                    </div>
                                    <span className="alert-tag">Vencido</span>
                                </div>
                            ))
                        ) : (
                            <p className="empty-state">No hay alertas de revisión pendientes.</p>
                        )}
                    </div>
                </div>

                {/* Documentos Recientes / Resumen por Área */}
                <div className="content-card chart-section">
                    <div className="card-header">
                        <h3>Distribución por Áreas</h3>
                    </div>
                    <div className="area-list">
                        {stats?.documentosPorArea?.map((area, idx) => (
                            <div key={idx} className="area-item">
                                <div className="area-info">
                                    <span className="area-name">{area.area}</span>
                                    <span className="area-count">{area.cantidad} docs</span>
                                </div>
                                <div className="area-bar-container">
                                    <div
                                        className="area-bar"
                                        style={{ width: `${(area.cantidad / stats.totalDocumentos) * 100}%` }}
                                    ></div>
                                </div>
                            </div>
                        ))}
                    </div>
                </div>
            </div>
        </div>
    );
};

export default Dashboard;
