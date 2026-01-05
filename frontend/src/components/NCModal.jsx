import { useState } from 'react';
import { X, Save, AlertTriangle, User, Calendar, Info } from 'lucide-react';
import { useAuth } from '../context/AuthContext';
import '../styles/NCModal.css';

const NCModal = ({ isOpen, onClose, onSave }) => {
    const { user } = useAuth();
    const [loading, setLoading] = useState(false);
    const [formData, setFormData] = useState({
        folio: '', // El backend podría generarlo, pero permitimos ingreso o sugerencia
        origen: 0,
        descripcionHallazgo: '',
        detectadoPor: user?.Nombre || '',
        fechaDeteccion: new Date().toISOString().split('T')[0],
        estado: 0
    });

    if (!isOpen) return null;

    const handleSubmit = async (e) => {
        e.preventDefault();
        setLoading(true);
        try {
            // Formatear para el backend (enums son ints)
            const payload = {
                ...formData,
                origen: parseInt(formData.origen),
                estado: parseInt(formData.estado),
                detectadoPor: formData.detectadoPor || user?.Nombre
            };
            await onSave(payload);
            setFormData({
                folio: '',
                origen: 0,
                descripcionHallazgo: '',
                detectadoPor: user?.Nombre || '',
                fechaDeteccion: new Date().toISOString().split('T')[0],
                estado: 0
            });
            onClose();
        } catch (error) {
            console.error('Error al guardar NC:', error);
            alert('Error al guardar la No Conformidad');
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="modal-overlay">
            <div className="modal-content nc-modal">
                <header className="modal-header">
                    <div className="header-title">
                        <div className="icon-badge warning">
                            <AlertTriangle size={20} />
                        </div>
                        <div>
                            <h2>Levantar No Conformidad</h2>
                            <p>Registro de hallazgo según NCh 2728</p>
                        </div>
                    </div>
                    <button className="close-btn" onClick={onClose}>
                        <X size={24} />
                    </button>
                </header>

                <form onSubmit={handleSubmit} className="modal-form">
                    <div className="form-section">
                        <div className="form-grid">
                            <div className="form-group">
                                <label>Folio (Opcional)</label>
                                <div className="input-with-icon">
                                    <Info size={18} />
                                    <input
                                        type="text"
                                        placeholder="Ej: NC-2026-001"
                                        value={formData.folio}
                                        onChange={(e) => setFormData({ ...formData, folio: e.target.value })}
                                    />
                                </div>
                                <small>Si se deja vacío, el sistema asignará uno.</small>
                            </div>

                            <div className="form-group">
                                <label>Fecha de Detección</label>
                                <div className="input-with-icon">
                                    <Calendar size={18} />
                                    <input
                                        type="date"
                                        required
                                        value={formData.fechaDeteccion}
                                        onChange={(e) => setFormData({ ...formData, fechaDeteccion: e.target.value })}
                                    />
                                </div>
                            </div>

                            <div className="form-group">
                                <label>Origen del Hallazgo</label>
                                <select
                                    required
                                    value={formData.origen}
                                    onChange={(e) => setFormData({ ...formData, origen: e.target.value })}
                                >
                                    <option value="0">Auditoría Interna</option>
                                    <option value="1">Auditoría Externa</option>
                                    <option value="2">Reclamo Cliente</option>
                                    <option value="3">Revisión por la Dirección</option>
                                    <option value="4">Incumplimiento de Proceso</option>
                                    <option value="5">Sugerencia de Mejora</option>
                                </select>
                            </div>

                            <div className="form-group">
                                <label>Detectado Por</label>
                                <div className="input-with-icon">
                                    <User size={18} />
                                    <input
                                        type="text"
                                        required
                                        value={formData.detectadoPor}
                                        onChange={(e) => setFormData({ ...formData, detectadoPor: e.target.value })}
                                    />
                                </div>
                            </div>
                        </div>

                        <div className="form-group full-width">
                            <label>Descripción del Hallazgo / Evidencia</label>
                            <textarea
                                required
                                rows="4"
                                placeholder="Describa detalladamente el incumplimiento o la situación detectada..."
                                value={formData.descripcionHallazgo}
                                onChange={(e) => setFormData({ ...formData, descripcionHallazgo: e.target.value })}
                            ></textarea>
                        </div>
                    </div>

                    <footer className="modal-footer">
                        <button type="button" className="btn-secondary" onClick={onClose}>
                            Cancelar
                        </button>
                        <button type="submit" className="btn-primary" disabled={loading}>
                            <Save size={18} />
                            <span>{loading ? 'Procesando...' : 'Registrar Hallazgo'}</span>
                        </button>
                    </footer>
                </form>
            </div>
        </div>
    );
};

export default NCModal;
