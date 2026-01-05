import { useState } from 'react';
import { X, Save, Clock, CheckCircle2, AlertCircle, Plus, ClipboardList, User, Calendar } from 'lucide-react';
import api from '../api/client';
import '../styles/NCModal.css';

const NCDetailsModal = ({ isOpen, onClose, nc, onUpdate }) => {
    const [loading, setLoading] = useState(false);
    const [nuevoEstado, setNuevoEstado] = useState(nc?.estado || 0);
    const [analisis, setAnalisis] = useState(nc?.analisisCausa || '');
    const [showAccionForm, setShowAccionForm] = useState(false);
    const [nuevaAccion, setNuevaAccion] = useState({
        descripcion: '',
        responsable: '',
        fechaCompromiso: new Date().toISOString().split('T')[0]
    });

    if (!isOpen || !nc) return null;

    const handleUpdateStatus = async (e) => {
        e.preventDefault();
        setLoading(true);
        try {
            const formData = new FormData();
            formData.append('nuevoEstado', nuevoEstado);
            formData.append('analisis', analisis);

            await api.patch(`/NoConformidades/${nc.id}/estado`, formData);
            onUpdate();
            alert('Estado actualizado correctamente');
        } catch (error) {
            console.error('Error al actualizar NC:', error);
            alert('Error al actualizar el estado');
        } finally {
            setLoading(false);
        }
    };

    const handleAddAccion = async (e) => {
        e.preventDefault();
        setLoading(true);
        try {
            await api.post(`/NoConformidades/${nc.id}/acciones`, nuevaAccion);
            onUpdate();
            setShowAccionForm(false);
            setNuevaAccion({ descripcion: '', responsable: '', fechaCompromiso: new Date().toISOString().split('T')[0] });
        } catch (error) {
            console.error('Error al agregar acción:', error);
            alert('Error al agregar acción correctiva');
        } finally {
            setLoading(false);
        }
    };

    const getOrigenNombre = (origen) => {
        const nombres = ['Auditoría Interna', 'Auditoría Externa', 'Reclamo Cliente', 'Revisión Dirección', 'Incumplimiento Proceso', 'Sugerencia Mejora'];
        return nombres[origen] || 'Otros';
    };

    return (
        <div className="modal-overlay">
            <div className="modal-content nc-details-modal">
                <header className="modal-header">
                    <div className="header-title">
                        <div className="icon-badge highlight">
                            <ClipboardList size={20} />
                        </div>
                        <div>
                            <h2>Gestión de Hallazgo: {nc.folio}</h2>
                            <p>{getOrigenNombre(nc.origen)} • Detectado por {nc.detectadoPor}</p>
                        </div>
                    </div>
                    <button className="close-btn" onClick={onClose}>
                        <X size={24} />
                    </button>
                </header>

                <div className="modal-body scrollable">
                    <section className="nc-info-section card-light">
                        <h3>Descripción del Hallazgo</h3>
                        <p>{nc.descripcionHallazgo}</p>
                        <div className="nc-meta">
                            <span><Calendar size={14} /> {new Date(nc.fechaDeteccion).toLocaleDateString()}</span>
                        </div>
                    </section>

                    <section className="nc-management-section">
                        <form onSubmit={handleUpdateStatus}>
                            <div className="form-grid">
                                <div className="form-group">
                                    <label>Estado del Hallazgo</label>
                                    <select
                                        value={nuevoEstado}
                                        onChange={(e) => setNuevoEstado(parseInt(e.target.value))}
                                        className="select-dark"
                                    >
                                        <option value="0">Abierta</option>
                                        <option value="1">En Análisis</option>
                                        <option value="2">En Implementación</option>
                                        <option value="3">Verificada</option>
                                        <option value="4">Cerrada / Eficaz</option>
                                    </select>
                                </div>
                            </div>
                            <div className="form-group">
                                <label>Análisis de Causa Raíz (¿Por qué sucedió?)</label>
                                <textarea
                                    rows="3"
                                    placeholder="Ingrese el análisis de causa para este hallazgo..."
                                    value={analisis}
                                    onChange={(e) => setAnalisis(e.target.value)}
                                ></textarea>
                            </div>
                            <button type="submit" className="btn-save-status" disabled={loading}>
                                <Save size={16} /> Actualizar Seguimiento
                            </button>
                        </form>
                    </section>

                    <section className="nc-actions-section">
                        <div className="section-header">
                            <h3>Acciones Correctivas / Preventivas</h3>
                            {!showAccionForm && (
                                <button className="btn-add-mini" onClick={() => setShowAccionForm(true)}>
                                    <Plus size={14} /> Nueva Acción
                                </button>
                            )}
                        </div>

                        {showAccionForm && (
                            <form className="accion-form card-light" onSubmit={handleAddAccion}>
                                <div className="form-group">
                                    <label>Descripción de la Acción</label>
                                    <input
                                        required
                                        type="text"
                                        placeholder="¿Qué se hará para corregir el hallazgo?"
                                        value={nuevaAccion.descripcion}
                                        onChange={(e) => setNuevaAccion({ ...nuevaAccion, descripcion: e.target.value })}
                                    />
                                </div>
                                <div className="form-grid">
                                    <div className="form-group">
                                        <label>Responsable</label>
                                        <input
                                            required
                                            type="text"
                                            value={nuevaAccion.responsable}
                                            onChange={(e) => setNuevaAccion({ ...nuevaAccion, responsable: e.target.value })}
                                        />
                                    </div>
                                    <div className="form-group">
                                        <label>Fecha Compromiso</label>
                                        <input
                                            required
                                            type="date"
                                            value={nuevaAccion.fechaCompromiso}
                                            onChange={(e) => setNuevaAccion({ ...nuevaAccion, fechaCompromiso: e.target.value })}
                                        />
                                    </div>
                                </div>
                                <div className="form-actions">
                                    <button type="button" className="btn-text" onClick={() => setShowAccionForm(false)}>Cancelar</button>
                                    <button type="submit" className="btn-primary-mini" disabled={loading}>Guardar Acción</button>
                                </div>
                            </form>
                        )}

                        <div className="acciones-list">
                            {nc.acciones && nc.acciones.length > 0 ? nc.acciones.map((acc, idx) => (
                                <div key={idx} className="accion-item">
                                    <div className="accion-status">
                                        {acc.fechaEjecucion ? <CheckCircle2 className="text-success" size={18} /> : <Clock className="text-warning" size={18} />}
                                    </div>
                                    <div className="accion-details">
                                        <p className="accion-desc">{acc.descripcion}</p>
                                        <div className="accion-meta">
                                            <span><User size={12} /> {acc.responsable}</span>
                                            <span><Calendar size={12} /> Vence: {new Date(acc.fechaCompromiso).toLocaleDateString()}</span>
                                        </div>
                                    </div>
                                </div>
                            )) : (
                                <p className="empty-msg">No se han definido acciones para esta No Conformidad.</p>
                            )}
                        </div>
                    </section>
                </div>
            </div>
        </div>
    );
};

export default NCDetailsModal;
