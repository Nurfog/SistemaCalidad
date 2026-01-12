import { useState } from 'react';
import { X, Save, FileText } from 'lucide-react';
import api from '../api/client';
import '../styles/DocumentModal.css';

const RenameModal = ({ isOpen, onClose, docId, currentTitulo, currentCodigo, onSave }) => {
    const [formData, setFormData] = useState({
        titulo: currentTitulo,
        codigo: currentCodigo
    });
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState('');

    if (!isOpen) return null;

    const handleSubmit = async (e) => {
        e.preventDefault();
        setLoading(true);
        setError('');

        try {
            await api.patch(`/Documentos/${docId}/renombrar`, formData);
            onSave();
            onClose();
        } catch (err) {
            setError(err.response?.data?.mensaje || 'Error al renombrar el documento.');
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="modal-overlay">
            <div className="modal-content card" style={{ maxWidth: '450px' }}>
                <header className="modal-header">
                    <div>
                        <h2>Renombrar Documento</h2>
                        <p className="text-muted" style={{ fontSize: '0.85rem' }}>ID: {docId}</p>
                    </div>
                    <button onClick={onClose} className="close-btn"><X size={20} /></button>
                </header>

                <form onSubmit={handleSubmit} className="modal-form">
                    {error && <div className="error-box">{error}</div>}

                    <div className="form-group">
                        <label>Código del Documento</label>
                        <input
                            type="text"
                            required
                            value={formData.codigo}
                            onChange={(e) => setFormData({ ...formData, codigo: e.target.value })}
                        />
                    </div>

                    <div className="form-group">
                        <label>Título del Documento</label>
                        <input
                            type="text"
                            required
                            value={formData.titulo}
                            onChange={(e) => setFormData({ ...formData, titulo: e.target.value })}
                        />
                    </div>

                    <footer className="modal-footer">
                        <button type="button" onClick={onClose} className="btn-secondary" disabled={loading}>
                            Cancelar
                        </button>
                        <button type="submit" className="btn-primary" disabled={loading}>
                            {loading ? 'Guardando...' : <><Save size={18} /> Guardar Cambios</>}
                        </button>
                    </footer>
                </form>
            </div>
        </div>
    );
};

export default RenameModal;
