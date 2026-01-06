import { useState, useEffect } from 'react';
import { History, CheckCircle, FileText, Clock, Download, TrendingUp, AlertCircle } from 'lucide-react';
import api from '../api/client';
import '../styles/PanelAuditoriaExterna.css';

const PanelAuditoriaExterna = () => {
    const [logs, setLogs] = useState([]);
    const [soluciones, setSoluciones] = useState([]);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        fetchData();
    }, []);

    const fetchData = async () => {
        setLoading(true);
        try {
            const [logsRes, solRes] = await Promise.all([
                api.get('/Auditoria'),
                api.get('/Auditoria/resumen-soluciones')
            ]);
            setLogs(logsRes.data);
            setSoluciones(solRes.data);
        } catch (error) {
            console.error('Error cargando datos de auditoría:', error);
        } finally {
            setLoading(false);
        }
    };

    if (loading) return <div className="p-8 text-center">Cargando panel de trazabilidad...</div>;

    return (
        <div className="audit-panel-container">
            <header className="page-header">
                <div>
                    <h1>Panel de Auditoría y Trazabilidad</h1>
                    <p>Monitoreo integral de ingresos, archivos y soluciones</p>
                </div>
                <div className="audit-badge">Auditoría Externa</div>
            </header>

            <div className="stats-strip">
                <div className="stat-card">
                    <TrendingUp className="text-success" />
                    <div>
                        <h3>{soluciones.length}</h3>
                        <span>Soluciones Implementadas</span>
                    </div>
                </div>
                <div className="stat-card">
                    <History className="text-blue" />
                    <div>
                        <h3>{logs.length}</h3>
                        <span>Registros de Actividad</span>
                    </div>
                </div>
            </div>

            <div className="audit-grid">
                {/* LINEA DE TIEMPO DE ACTIVIDAD */}
                <div className="audit-card">
                    <div className="card-header">
                        <Clock size={20} />
                        <h2>Registro de Actividad Reciente</h2>
                    </div>
                    <div className="logs-list">
                        {logs.map((log) => (
                            <div key={log.id} className="log-item">
                                <div className="log-time">
                                    {new Date(log.fecha).toLocaleDateString()}
                                    <small>{new Date(log.fecha).toLocaleTimeString()}</small>
                                </div>
                                <div className="log-content">
                                    <span className={`log-badge ${log.accion.toLowerCase()}`}>{log.accion}</span>
                                    <p><strong>{log.usuario}</strong> realizó una acción en <b>{log.entidad}</b></p>
                                    <small>{log.detalle}</small>
                                </div>
                            </div>
                        ))}
                    </div>
                </div>

                {/* RESUMEN DE SOLUCIONES */}
                <div className="audit-card">
                    <div className="card-header">
                        <CheckCircle size={20} />
                        <h2>Últimas Soluciones (NC Cerradas)</h2>
                    </div>
                    <div className="solutions-list">
                        {soluciones.map((nc) => (
                            <div key={nc.folio} className="solution-item">
                                <header>
                                    <span className="nc-folio">{nc.folio}</span>
                                    <span className="nc-date">{new Date(nc.fechaCierre).toLocaleDateString()}</span>
                                </header>
                                <h3>{nc.descripcionHallazgo}</h3>
                                <div className="solution-detail">
                                    <strong>Causa:</strong> {nc.analisisCausa}
                                </div>
                                <div className="solution-actions">
                                    {nc.acciones.map((acc, i) => (
                                        <div key={i} className="mini-action">
                                            <CheckCircle size={12} className="text-success" />
                                            {acc.descripcion}
                                        </div>
                                    ))}
                                </div>
                            </div>
                        ))}
                        {soluciones.length === 0 && <p className="text-muted p-4">No hay soluciones cerradas recientemente.</p>}
                    </div>
                </div>
            </div>
        </div>
    );
};

export default PanelAuditoriaExterna;
