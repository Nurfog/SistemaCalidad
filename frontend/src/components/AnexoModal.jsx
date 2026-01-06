import { useState } from 'react';
import { X, Upload, CheckCircle } from 'lucide-react';
import '../styles/DocumentModal.css'; // Reutilizamos estilos base de modales

const AnexoModal = ({ isOpen, onClose, onSave }) => {
    const [formData, setFormData] = useState({
        nombre: '',
        codigo: '',
        descripcion: '',
        esObligatorio: false
    });
    const [archivo, setArchivo] = useState(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState('');

    if (!isOpen) return null;

    const handleSubmit = async (e) => {
        e.preventDefault();
        if (!archivo) {
            setError('Debe seleccionar un archivo para la plantilla.');
            return;
        }

        setLoading(true);
        setError('');

        const data = new FormData();
        data.append('nombre', formData.nombre);
        data.append('codigo', formData.codigo);
        data.append('descripcion', formData.descripcion || '');
        data.append('esObligatorio', formData.esObligatorio);
        data.append('archivo', archivo);

        try {
            await onSave(data);
            onClose();
            // Reset form
            setFormData({ nombre: '', codigo: '', descripcion: '', esObligatorio: false });
            setArchivo(null);
        } catch (err) {
            setError(err.response?.data || 'Error al guardar la plantilla.');
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="modal-overlay">
            <div className="modal-content card" style={{ maxWidth: '500px' }}>
                <header className="modal-header">
                    <h2>Nueva Plantilla / Anexo</h2>
                    <button onClick={onClose} className="close-btn"><X size={20} /></button>
                </header>

                <form onSubmit={handleSubmit} className="modal-form">
                    {error && <div className="error-box">{error}</div>}

                    <div className="form-group">
                        <label>Nombre de la Plantilla</label>
                        <input
                            type="text"
                            required
                            placeholder="Ej: Acta de Reunión de Directorio"
                            value={formData.nombre}
                            onChange={(e) => setFormData({ ...formData, nombre: e.target.value })}
                        />
                    </div>

                    <div className="form-group">
                        <label>Código de Referencia</label>
                        <input
                            type="text"
                            required
                            placeholder="Ej: FOR-SGC-001"
                            value={formData.codigo}
                            onChange={(e) => setFormData({ ...formData, codigo: e.target.value })}
                        />
                    </div>

                    <div className="form-group">
                        <label>Descripción (Opcional)</label>
                        <textarea
                            placeholder="Uso sugerido o referencia normativa..."
                            value={formData.descripcion}
                            onChange={(e) => setFormData({ ...formData, descripcion: e.target.value })}
                            rows={3}
                        />
                    </div>

                    <div className="form-group checkbox-group">
                        <label className="checkbox-label">
                            <input
                                type="checkbox"
                                checked={formData.esObligatorio}
                                onChange={(e) => setFormData({ ...formData, esObligatorio: e.target.checked })}
                            />
                            <span>Es formato obligatorio de la norma</span>
                        </label>
                    </div>

                    <div className={`file-upload-zone ${archivo ? 'has-file' : ''}`}>
                        <input
                            type="file"
                            id="anexo-file"
                            className="file-input"
                            onChange={(e) => setArchivo(e.target.files[0])}
                        />
                        <label htmlFor="anexo-file" className="file-label">
                            {archivo ? (
                                <>
                                    <CheckCircle size={32} className="upload-icon-success" />
                                    <span>{archivo.name}</span>
                                </>
                            ) : (
                                <>
                                    <Upload size={32} className="upload-icon" />
                                    <span>Seleccionar Archivo</span>
                                    <span className="file-info">DOCX, XLSX, PDF</span>
                                </>
                            )}
                        </label>
                    </div>

                    <footer className="modal-footer">
                        <button type="button" onClick={onClose} className="btn-secondary" disabled={loading}>
                            Cancelar
                        </button>
                        <button type="submit" className="btn-primary" disabled={loading}>
                            {loading ? 'Subiendo...' : 'Guardar Plantilla'}
                        </button>
                    </footer>
                </form>
            </div>
        </div>
    );
};

export default AnexoModal;
