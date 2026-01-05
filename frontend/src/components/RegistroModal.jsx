import { useState } from 'react';
import { X, Save, FileCheck, HardDrive, Calendar, BookOpen, Upload } from 'lucide-react';
import '../styles/DocumentModal.css'; // Reutilizamos base de estilos

const RegistroModal = ({ isOpen, onClose, onSave }) => {
    const [loading, setLoading] = useState(false);
    const [archivo, setArchivo] = useState(null);
    const [formData, setFormData] = useState({
        nombre: '',
        identificador: '',
        anosRetencion: 5
    });

    if (!isOpen) return null;

    const handleSubmit = async (e) => {
        e.preventDefault();
        if (!archivo) {
            alert('Debe adjuntar el archivo de evidencia.');
            return;
        }

        setLoading(true);
        try {
            const data = new FormData();
            data.append('nombre', formData.nombre);
            data.append('identificador', formData.identificador);
            data.append('anosRetencion', formData.anosRetencion);
            data.append('archivo', archivo);

            await onSave(data);
            setFormData({ nombre: '', identificador: '', anosRetencion: 5 });
            setArchivo(null);
            onClose();
        } catch (error) {
            console.error('Error al guardar registro:', error);
            alert('Error al guardar el registro de calidad.');
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="modal-overlay">
            <div className="modal-content">
                <header className="modal-header">
                    <div className="header-title">
                        <div className="icon-badge highlight">
                            <FileCheck size={20} />
                        </div>
                        <div>
                            <h2>Archivar Nuevo Registro</h2>
                            <p>Registro de evidencia operativa NCh 2728</p>
                        </div>
                    </div>
                    <button className="close-btn" onClick={onClose}>
                        <X size={24} />
                    </button>
                </header>

                <form onSubmit={handleSubmit} className="modal-form">
                    <div className="form-section">
                        <div className="form-group">
                            <label>Nombre del Registro / Evidencia</label>
                            <input
                                type="text"
                                required
                                placeholder="Ej: Lista de Asistencia - Curso Excel"
                                value={formData.nombre}
                                onChange={(e) => setFormData({ ...formData, nombre: e.target.value })}
                            />
                        </div>

                        <div className="form-row">
                            <div className="form-group">
                                <label>Identificador / Correlativo</label>
                                <input
                                    type="text"
                                    required
                                    placeholder="Ej: REG-OP-001"
                                    value={formData.identificador}
                                    onChange={(e) => setFormData({ ...formData, identificador: e.target.value })}
                                />
                            </div>
                            <div className="form-group">
                                <label>Años de Retención</label>
                                <input
                                    type="number"
                                    required
                                    min="1"
                                    max="50"
                                    value={formData.anosRetencion}
                                    onChange={(e) => setFormData({ ...formData, anosRetencion: e.target.value })}
                                />
                            </div>
                        </div>

                        <div className="form-group">
                            <label>Adjuntar Archivo (PDF, Excel, Scan)</label>
                            <div className={`file-upload-zone ${archivo ? 'has-file' : ''}`}>
                                <input
                                    type="file"
                                    id="archivo-reg"
                                    onChange={(e) => setArchivo(e.target.files[0])}
                                    style={{ display: 'none' }}
                                />
                                <label htmlFor="archivo-reg" className="file-label">
                                    <Upload size={24} />
                                    <span>{archivo ? archivo.name : 'Seleccionar archivo o arrastra aquí'}</span>
                                </label>
                            </div>
                        </div>
                    </div>

                    <footer className="modal-footer">
                        <button type="button" className="btn-secondary" onClick={onClose}>
                            Cancelar
                        </button>
                        <button type="submit" className="btn-primary" disabled={loading}>
                            <Save size={18} />
                            <span>{loading ? 'Subiendo...' : 'Archivar Registro'}</span>
                        </button>
                    </footer>
                </form>
            </div>
        </div>
    );
};

export default RegistroModal;
