import { useState } from 'react';
import { X, Upload, CheckCircle } from 'lucide-react';
import api from '../api/client';
import '../styles/DocumentModal.css';

const RevisionModal = ({ isOpen, onClose, docId, docTitulo, onSave }) => {
    const [archivo, setArchivo] = useState(null);
    const [descripcionCambio, setDescripcionCambio] = useState('');
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState('');

    if (!isOpen) return null;

    const handleSubmit = async (e) => {
        e.preventDefault();
        if (!archivo) {
            setError('Debe seleccionar un archivo para la nueva versión.');
            return;
        }

        setLoading(true);
        setError('');

        const data = new FormData();
        data.append('descripcionCambio', descripcionCambio || 'Nueva revisión cargada por administrador');
        data.append('archivo', archivo);

        try {
            await api.post(`/Documentos/${docId}/revision`, data, {
                headers: { 'Content-Type': 'multipart/form-data' }
            });
            onSave();
            onClose();
        } catch (err) {
            setError(err.response?.data || 'Error al subir la revisión.');
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="modal-overlay">
            <div className="modal-content card">
                <header className="modal-header">
                    <h2>Subir Nueva Versión</h2>
                    <button onClick={onClose} className="close-btn"><X size={20} /></button>
                </header>

                <div className="modal-info" style={{ marginBottom: '1rem', color: 'var(--text-secondary)' }}>
                    Actualizando documento: <strong>{docTitulo}</strong>
                </div>

                <form onSubmit={handleSubmit} className="modal-form">
                    {error && <div className="error-box">{error}</div>}

                    <div className="form-group">
                        <label>Motivo del Cambio / Descripción</label>
                        <textarea
                            rows="3"
                            required
                            placeholder="Describa los cambios realizados..."
                            value={descripcionCambio}
                            onChange={(e) => setDescripcionCambio(e.target.value)}
                        />
                    </div>

                    <div className={`file-upload-zone ${archivo ? 'has-file' : ''}`}>
                        <input
                            type="file"
                            id="revision-file"
                            className="file-input"
                            onChange={(e) => setArchivo(e.target.files[0])}
                        />
                        <label htmlFor="revision-file" className="file-label">
                            {archivo ? (
                                <>
                                    <CheckCircle size={32} className="upload-icon-success" />
                                    <span>{archivo.name}</span>
                                </>
                            ) : (
                                <>
                                    <Upload size={32} className="upload-icon" />
                                    <span>Seleccionar Archivo PDF/Word</span>
                                </>
                            )}
                        </label>
                    </div>

                    <footer className="modal-footer">
                        <button type="button" onClick={onClose} className="btn-secondary" disabled={loading}>
                            Cancelar
                        </button>
                        <button type="submit" className="btn-primary" disabled={loading}>
                            {loading ? 'Subiendo...' : 'Publicar Versión'}
                        </button>
                    </footer>
                </form>
            </div>
        </div>
    );
};

export default RevisionModal;
