import { useState, useEffect } from 'react';
import { Search, Download, Plus, FileText, AlertCircle, Trash2, ExternalLink } from 'lucide-react';
import api from '../api/client';
import { useAuth } from '../context/AuthContext';
import AnexoModal from '../components/AnexoModal';
import TemplateFormModal from '../components/TemplateFormModal';
import '../styles/Anexos.css';

const Anexos = () => {
    const { user } = useAuth();
    const [anexos, setAnexos] = useState([]);
    const [loading, setLoading] = useState(true);
    const [buscar, setBuscar] = useState('');
    const [isModalOpen, setIsModalOpen] = useState(false);
    const [isTemplateModalOpen, setIsTemplateModalOpen] = useState(false);
    const [selectedTemplate, setSelectedTemplate] = useState(null);

    const fetchAnexos = async () => {
        setLoading(true);
        try {
            const res = await api.get('/Anexos', { params: { buscar: buscar || undefined } });
            setAnexos(res.data);
        } catch (error) {
            console.error('Error al cargar anexos:', error);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        const timer = setTimeout(() => {
            fetchAnexos();
        }, 300);
        return () => clearTimeout(timer);
    }, [buscar]);

    const handleSaveAnexo = async (formData) => {
        await api.post('/Anexos', formData, {
            headers: { 'Content-Type': 'multipart/form-data' }
        });
        fetchAnexos();
    };

    const handleDownload = async (id, nombre) => {
        try {
            const response = await api.get(`/Anexos/${id}/descargar`, {
                responseType: 'blob'
            });
            const url = window.URL.createObjectURL(new Blob([response.data]));
            const link = document.createElement('a');
            link.href = url;
            link.setAttribute('download', nombre);
            document.body.appendChild(link);
            link.click();
            link.remove();
        } catch (error) {
            console.error('Error descargando:', error);
            let detail = 'Error desconocido';

            if (error.response?.data instanceof Blob) {
                // Si la respuesta es un Blob (común en descargas), lo leemos como texto
                const text = await error.response.data.text();
                detail = text || detail;
            } else if (typeof error.response?.data === 'string') {
                detail = error.response.data;
            } else if (error.response?.data?.mensaje) {
                detail = error.response.data.mensaje;
            }

            alert('Error al descargar el anexo: ' + detail);
        }
    };

    const handleDelete = async (id, nombre) => {
        if (!window.confirm(`¿Estás seguro de que deseas eliminar la plantilla "${nombre}"? Esta acción no se puede deshacer.`)) {
            return;
        }

        try {
            await api.delete(`/Anexos/${id}`);
            fetchAnexos(); // Recargar lista
        } catch (error) {
            console.error('Error al eliminar:', error);
            alert('No se pudo eliminar el anexo. Inténtalo de nuevo.');
        }
    };

    return (
        <div className="anexos-page">
            <header className="page-header">
                <div>
                    <h1>Anexos y Plantillas Maestras</h1>
                    <p>Formatos obligatorios y documentos complementarios del SGC</p>
                </div>
                {(user?.Rol?.includes('Administrador') || user?.Rol?.includes('Escritor')) && (
                    <button className="btn-primary" onClick={() => setIsModalOpen(true)}>
                        <Plus size={20} />
                        <span>Nueva Plantilla</span>
                    </button>
                )}
            </header>

            <section className="toolbar">
                <div className="search-box">
                    <Search size={20} className="search-icon" />
                    <input
                        type="text"
                        placeholder="Buscar por código o nombre..."
                        value={buscar}
                        onChange={(e) => setBuscar(e.target.value)}
                    />
                </div>
            </section>

            <div className="anexos-grid">
                {loading ? (
                    <div className="loading-state">Cargando anexos...</div>
                ) : anexos.length > 0 ? (
                    anexos.map((anexo) => (
                        <div key={anexo.id} className="anexo-card card">
                            <div className="anexo-icon-wrapper">
                                <FileText size={32} className="anexo-icon" />
                                <span className={`format-badge ${anexo.formato.toLowerCase()}`}>
                                    {anexo.formato}
                                </span>
                            </div>
                            <div className="anexo-info">
                                <small className="anexo-code">{anexo.codigo}</small>
                                <h3>{anexo.nombre}</h3>
                                <p className="anexo-desc">{anexo.descripcion || 'Sin descripción'}</p>
                                {anexo.esObligatorio && (
                                    <span className="obligatory-tag">
                                        <AlertCircle size={12} /> Obligatorio
                                    </span>
                                )}
                            </div>
                            <footer className="anexo-actions">
                                <button
                                    className="btn-download"
                                    onClick={() => handleDownload(anexo.id, `${anexo.nombre}.${anexo.formato.toLowerCase()}`)}
                                >
                                    <Download size={18} />
                                    <span>Descargar</span>
                                </button>
                                {anexo.formato.toUpperCase() === 'DOCX' && (
                                    <button
                                        className="btn-generate"
                                        onClick={() => {
                                            setSelectedTemplate(anexo);
                                            setIsTemplateModalOpen(true);
                                        }}
                                        title="Generar documento desde esta plantilla"
                                    >
                                        <Plus size={18} />
                                        <span>Generar</span>
                                    </button>
                                )}
                                {user?.Rol === 'Administrador' && (
                                    <button
                                        className="btn-delete-icon"
                                        onClick={() => handleDelete(anexo.id, anexo.nombre)}
                                        title="Eliminar plantilla"
                                    >
                                        <Trash2 size={18} />
                                    </button>
                                )}
                            </footer>
                        </div>
                    ))
                ) : (
                    <div className="empty-state">
                        <FileText size={48} className="text-muted" />
                        <p>No se encontraron anexos o plantillas.</p>
                    </div>
                )}
            </div>

            <AnexoModal
                isOpen={isModalOpen}
                onClose={() => setIsModalOpen(false)}
                onSave={handleSaveAnexo}
            />
            <TemplateFormModal
                isOpen={isTemplateModalOpen}
                onClose={() => setIsTemplateModalOpen(false)}
                template={selectedTemplate}
            />
        </div>
    );
};

export default Anexos;
