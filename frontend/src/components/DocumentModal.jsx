import { useState } from 'react';
import { X, Upload, FileText, CheckCircle } from 'lucide-react';
import '../styles/DocumentModal.css';

const DocumentModal = ({ isOpen, onClose, onSave, nombreCarpeta }) => {
    const [formData, setFormData] = useState({
        titulo: '',
        codigo: '',
        tipo: '0', // Manual
        area: '0',  // Direccion
        numeroRevision: '1' // Valor por defecto
    });
    const [archivo, setArchivo] = useState(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState('');

    if (!isOpen) return null;

    const handleSubmit = async (e) => {
        e.preventDefault();
        if (!archivo) {
            setError('Debe seleccionar un archivo para el documento.');
            return;
        }

        setLoading(true);
        setError('');

        const data = new FormData();
        data.append('titulo', formData.titulo);
        data.append('codigo', formData.codigo);
        data.append('tipo', formData.tipo);
        data.append('area', formData.area);
        data.append('numeroRevision', formData.numeroRevision);
        data.append('archivo', archivo);

        try {
            await onSave(data);
            onClose();
        } catch (err) {
            setError(err.response?.data || 'Error al guardar el documento.');
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="modal-overlay">
            <div className="modal-content card">
                <header className="modal-header">
                    <div>
                        <h2>Nuevo Documento</h2>
                        {nombreCarpeta && <p className="text-muted" style={{ fontSize: '0.85rem' }}>Destino: <strong>{nombreCarpeta}</strong></p>}
                    </div>
                    <button onClick={onClose} className="close-btn"><X size={20} /></button>
                </header>

                <form onSubmit={handleSubmit} className="modal-form">
                    {error && <div className="error-box">{error}</div>}

                    <div className="form-group">
                        <label>Título del Documento</label>
                        <input
                            type="text"
                            required
                            placeholder="Ej: Manual de Calidad"
                            value={formData.titulo}
                            onChange={(e) => setFormData({ ...formData, titulo: e.target.value })}
                        />
                    </div>

                    <div className="form-row">
                        <div className="form-group">
                            <label>Código Correlativo</label>
                            <input
                                type="text"
                                required
                                placeholder="MNC-SGC-001"
                                value={formData.codigo}
                                onChange={(e) => setFormData({ ...formData, codigo: e.target.value })}
                            />
                        </div>
                        <div className="form-group">
                            <label>Tipo</label>
                            <select
                                value={formData.tipo}
                                onChange={(e) => setFormData({ ...formData, tipo: e.target.value })}
                            >
                                <option value="0">Manual</option>
                                <option value="1">Procedimiento</option>
                                <option value="2">Instructivo</option>
                                <option value="3">Anexo</option>
                                <option value="4">Formulario</option>
                            </select>
                        </div>
                    </div>

                    <div className="form-row">
                        <div className="form-group">
                            <label>Área o Proceso</label>
                            <select
                                value={formData.area}
                                onChange={(e) => setFormData({ ...formData, area: e.target.value })}
                            >
                                <option value="0">Dirección</option>
                                <option value="1">Comercial</option>
                                <option value="2">Operativa</option>
                                <option value="3">Apoyo</option>
                                <option value="4">Administrativa</option>
                            </select>
                        </div>
                        <div className="form-group">
                            <label>N° de Revisión Inicial</label>
                            <input
                                type="number"
                                required
                                min="0"
                                value={formData.numeroRevision}
                                onChange={(e) => setFormData({ ...formData, numeroRevision: e.target.value })}
                            />
                            <span className="field-hint">Use 1 para nuevos, o el número actual si es migración.</span>
                        </div>
                    </div>

                    <div className={`file-upload-zone ${archivo ? 'has-file' : ''}`}>
                        <input
                            type="file"
                            id="doc-file"
                            className="file-input"
                            onChange={(e) => setArchivo(e.target.files[0])}
                        />
                        <label htmlFor="doc-file" className="file-label">
                            {archivo ? (
                                <>
                                    <CheckCircle size={32} className="upload-icon-success" />
                                    <span>{archivo.name}</span>
                                    <span className="file-info">Haga clic para cambiar el archivo</span>
                                </>
                            ) : (
                                <>
                                    <Upload size={32} className="upload-icon" />
                                    <span>Seleccionar Archivo</span>
                                    <span className="file-info">PDF, DOCX, XLSX (Max 10MB)</span>
                                </>
                            )}
                        </label>
                    </div>

                    <footer className="modal-footer">
                        <button type="button" onClick={onClose} className="btn-secondary" disabled={loading}>
                            Cancelar
                        </button>
                        <button type="submit" className="btn-primary" disabled={loading}>
                            {loading ? 'Subiendo...' : 'Crear Documento'}
                        </button>
                    </footer>
                </form>
            </div>
        </div>
    );
};

export default DocumentModal;
