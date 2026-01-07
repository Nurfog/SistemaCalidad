import { useState, useEffect } from 'react';
import { X, FileText, Send, Download, Loader2 } from 'lucide-react';
import api from '../api/client';
import '../styles/TemplateFormModal.css';

const TemplateFormModal = ({ isOpen, onClose, template }) => {
    const [tags, setTags] = useState([]);
    const [valores, setValores] = useState({});
    const [loading, setLoading] = useState(false);
    const [generating, setGenerating] = useState(false);

    useEffect(() => {
        if (isOpen && template) {
            fetchTags();
        }
    }, [isOpen, template]);

    const fetchTags = async () => {
        setLoading(true);
        try {
            const res = await api.get(`/Anexos/${template.id}/tags`);
            setTags(res.data);
            const initialValores = {};
            res.data.forEach(tag => {
                initialValores[tag] = '';
            });
            setValores(initialValores);
        } catch (error) {
            console.error('Error fetching tags:', error);
            alert('No se pudieron detectar etiquetas en esta plantilla.');
        } finally {
            setLoading(false);
        }
    };

    const handleChange = (tag, value) => {
        setValores(prev => ({ ...prev, [tag]: value }));
    };

    const handleGenerate = async (e) => {
        e.preventDefault();
        setGenerating(true);
        try {
            const response = await api.post(`/Anexos/${template.id}/generar`, valores, {
                responseType: 'blob'
            });

            const fileName = `${template.nombre}_Generado.docx`;
            const url = window.URL.createObjectURL(new Blob([response.data]));
            const link = document.createElement('a');
            link.href = url;
            link.setAttribute('download', fileName);
            document.body.appendChild(link);
            link.click();
            link.remove();

            onClose();
        } catch (error) {
            console.error('Error generating document:', error);
            alert('Error al generar el documento.');
        } finally {
            setGenerating(false);
        }
    };

    if (!isOpen) return null;

    return (
        <div className="modal-overlay">
            <div className="modal-content template-modal">
                <header className="modal-header">
                    <div className="flex items-center gap-2">
                        <FileText className="text-primary" />
                        <h2>Generar desde Plantilla</h2>
                    </div>
                    <button className="btn-close" onClick={onClose}><X size={20} /></button>
                </header>

                <div className="modal-body">
                    <div className="template-summary mb-4">
                        <p><strong>Plantilla:</strong> {template.nombre}</p>
                        <p className="text-muted text-sm">Completa los campos para generar el archivo personalizado.</p>
                    </div>

                    {loading ? (
                        <div className="p-8 text-center">
                            <Loader2 size={32} className="animate-spin mx-auto mb-2 text-primary" />
                            <p>Escaneando etiquetas...</p>
                        </div>
                    ) : tags.length > 0 ? (
                        <form id="templateForm" onSubmit={handleGenerate} className="tags-form">
                            {tags.map(tag => (
                                <div key={tag} className="form-group">
                                    <label>{tag.replace(/_/g, ' ')}</label>
                                    <input
                                        type="text"
                                        value={valores[tag]}
                                        onChange={(e) => handleChange(tag, e.target.value)}
                                        placeholder={`Escribe ${tag}...`}
                                        required
                                    />
                                </div>
                            ))}
                        </form>
                    ) : (
                        <div className="p-4 bg-orange-50 text-orange-700 rounded-lg text-sm">
                            No se detectaron etiquetas dinámicas ({{ ETIQUETA }}) en esta plantilla DOCX.
                            El documento se generará como una copia exacta.
                        </div>
                    )}
                </div>

                <footer className="modal-footer">
                    <button className="btn-secondary" onClick={onClose} disabled={generating}>Cancelar</button>
                    <button
                        form="templateForm"
                        type="submit"
                        className="btn-primary"
                        disabled={generating}
                    >
                        {generating ? (
                            <>
                                <Loader2 size={18} className="animate-spin" />
                                <span>Generando...</span>
                            </>
                        ) : (
                            <>
                                <Download size={18} />
                                <span>Generar y Descargar</span>
                            </>
                        )}
                    </button>
                </footer>
            </div>
        </div>
    );
};

export default TemplateFormModal;
