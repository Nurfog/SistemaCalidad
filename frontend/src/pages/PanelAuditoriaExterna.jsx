import { useState, useEffect } from 'react';
import { History, CheckCircle, FileText, Clock, Download, TrendingUp, AlertCircle } from 'lucide-react';
import api from '../api/client';
import '../styles/PanelAuditoriaExterna.css';

const PanelAuditoriaExterna = () => {
    const [logs, setLogs] = useState([]);
    const [soluciones, setSoluciones] = useState([]);
    const [loading, setLoading] = useState(true);
    const [searchTerm, setSearchTerm] = useState('');
    const [filterEntity, setFilterEntity] = useState('Todos');

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

    const entidadesDisponibles = ['Todos', ...new Set(logs.map(l => l.entidad))];

    const logsFiltrados = logs.filter(log => {
        const matchesSearch =
            log.usuario.toLowerCase().includes(searchTerm.toLowerCase()) ||
            log.detalle.toLowerCase().includes(searchTerm.toLowerCase()) ||
            log.accion.toLowerCase().includes(searchTerm.toLowerCase());

        const matchesEntity = filterEntity === 'Todos' || log.entidad === filterEntity;

        return matchesSearch && matchesEntity;
    });

    const handleExport = () => {
        // Simulación de exportación profesional
        alert('Generando reporte PDF de trazabilidad... El archivo se descargará en unos momentos.');
    };

    if (loading) return <div className="p-8 text-center">Cargando panel de trazabilidad...</div>;

    return (
        <div className="audit-panel-container">
            <header className="page-header">
                <div>
                    <div className="flex items-center gap-2 mb-1">
                        <History className="text-blue" size={24} />
                        <h1>Centro de Trazabilidad y Auditoría</h1>
                    </div>
                    <p>Monitoreo integral de cumplimiento normativo y evidencia de control</p>
                </div>
                <div className="flex gap-4 items-center">
                    <button onClick={handleExport} className="export-report-btn">
                        <Download size={18} />
                        Exportar Evidencia
                    </button>
                    <div className="audit-badge">Auditoría Externa</div>
                </div>
            </header>

            <div className="audit-controls-strip">
                <div className="search-box">
                    <FileText size={18} />
                    <input
                        type="text"
                        placeholder="Buscar por usuario, acción o detalle..."
                        value={searchTerm}
                        onChange={(e) => setSearchTerm(e.target.value)}
                    />
                </div>
                <div className="filter-group">
                    <span>Módulo:</span>
                    <select value={filterEntity} onChange={(e) => setFilterEntity(e.target.value)}>
                        {entidadesDisponibles.map(ent => (
                            <option key={ent} value={ent}>{ent}</option>
                        ))}
                    </select>
                </div>
            </div>

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
                        <h3>{logsFiltrados.length}</h3>
                        <span>Registros Filtrados</span>
                    </div>
                </div>
            </div>

            <div className="audit-grid">
                {/* LINEA DE TIEMPO DE ACTIVIDAD */}
                <div className="audit-card">
                    <div className="card-header">
                        <Clock size={20} />
                        <h2>Línea de Tiempo de Evidencia</h2>
                    </div>
                    <div className="logs-list">
                        {logsFiltrados.map((log) => (
                            <div key={log.id} className="log-item">
                                <div className="log-time">
                                    {new Date(log.fecha).toLocaleDateString()}
                                    <small>{new Date(log.fecha).toLocaleTimeString()}</small>
                                </div>
                                <div className="log-icon-container">
                                    <div className={`log-dot ${log.accion.toLowerCase()}`}></div>
                                    <div className="log-connector"></div>
                                </div>
                                <div className="log-content">
                                    <div className="flex justify-between items-start mb-1">
                                        <span className={`log-badge ${log.accion.toLowerCase()}`}>{log.accion}</span>
                                        <span className="log-entity-tag">{log.entidad}</span>
                                    </div>
                                    <p><strong>{log.usuario}</strong> realizó una acción de control</p>
                                    <div className="log-detail-box">
                                        {log.detalle}
                                    </div>
                                </div>
                            </div>
                        ))}
                        {logsFiltrados.length === 0 && (
                            <div className="empty-state p-8 text-center text-muted">
                                <AlertCircle size={40} className="mx-auto mb-2 opacity-20" />
                                <p>No se encontraron registros que coincidan con los criterios.</p>
                            </div>
                        )}
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
