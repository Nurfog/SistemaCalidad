import { useState, useEffect } from 'react';
import ReactQuill from 'react-quill-new';
import 'react-quill-new/dist/quill.snow.css';
import { Save, FileText, ArrowLeft, Loader2, Search } from 'lucide-react';
import api from '../api/client';
import { useNavigate, useParams } from 'react-router-dom';
import '../styles/RedactorDocumento.css';

const RedactorDocumento = () => {
    const navigate = useNavigate();
    const { baseId } = useParams();
    const [loading, setLoading] = useState(false);
    const [buscandoBase, setBuscandoBase] = useState(false);
    const [documentos, setDocumentos] = useState([]);

    const [formData, setFormData] = useState({
        id: null,
        titulo: '',
        codigo: '',
        tipo: 1, // Procedimiento por defecto
        area: 2, // Operacional por defecto
        contenidoHtml: '',
        descripcionCambio: ''
    });

    useEffect(() => {
        fetchDocumentos();
        if (baseId) {
            cargarBase(baseId);
        }
    }, [baseId]);

    const cargarBase = async (id) => {
        try {
            const res = await api.get('/Documentos'); // Podríamos tener un get por ID pero reusemos la lista o busquemos
            const doc = res.data.find(d => d.id === parseInt(id));
            if (doc) handleSelectBase(doc);
        } catch (error) {
            console.error('Error al cargar base:', error);
        }
    };

    const fetchDocumentos = async () => {
        try {
            const res = await api.get('/Documentos');
            setDocumentos(res.data);
        } catch (error) {
            console.error('Error al cargar documentos:', error);
        }
    };

    const handleSelectBase = (doc) => {
        setFormData({
            ...formData,
            id: doc.id,
            titulo: doc.titulo,
            codigo: doc.codigo,
            tipo: doc.tipo,
            area: doc.area,
            descripcionCambio: `Actualización de versión`
        });
        setBuscandoBase(false);
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        if (!formData.contenidoHtml || formData.contenidoHtml === '<p><br></p>') {
            alert('El contenido del documento no puede estar vacío.');
            return;
        }

        setLoading(true);
        try {
            await api.post('/Documentos/redactar', formData);
            alert('Documento guardado exitosamente. Se ha generado una nueva versión en borrador.');
            navigate('/documentos');
        } catch (error) {
            console.error('Error al guardar:', error);
            alert('Ocurrió un error al procesar el documento. Revisa la consola.');
        } finally {
            setLoading(false);
        }
    };

    // Módulos de Quill
    const modules = {
        toolbar: [
            [{ 'header': [1, 2, 3, false] }],
            ['bold', 'italic', 'underline', 'strike'],
            [{ 'list': 'ordered' }, { 'list': 'bullet' }],
            ['link', 'image', 'table'],
            ['clean']
        ],
    };

    return (
        <div className="redactor-container">
            <header className="page-header">
                <div className="flex items-center gap-4">
                    <button className="btn-icon" onClick={() => navigate(-1)}><ArrowLeft size={20} /></button>
                    <div>
                        <h1>Redactor de Documentos SGC</h1>
                        <p>Genera documentos oficiales con formato estandarizado</p>
                    </div>
                </div>
                <div className="flex gap-2">
                    <button
                        className="btn-secondary"
                        onClick={() => setBuscandoBase(true)}
                    >
                        <Search size={18} />
                        <span>Usar como Base</span>
                    </button>
                    <button
                        className="btn-primary"
                        onClick={handleSubmit}
                        disabled={loading}
                    >
                        {loading ? <Loader2 size={18} className="animate-spin" /> : <Save size={18} />}
                        <span>{formData.id ? 'Guardar Nueva Versión' : 'Publicar Documento'}</span>
                    </button>
                </div>
            </header>

            <div className="redactor-grid">
                {/* Panel de Metadatos */}
                <aside className="metadata-panel card">
                    <h3>Metadatos Oficiales</h3>
                    <div className="form-group">
                        <label>Código del Documento</label>
                        <input
                            type="text"
                            placeholder="Ej: PR-OPER-001"
                            value={formData.codigo}
                            onChange={(e) => setFormData({ ...formData, codigo: e.target.value })}
                        />
                    </div>
                    <div className="form-group">
                        <label>Título del Documento</label>
                        <input
                            type="text"
                            placeholder="Nombre del procedimiento..."
                            value={formData.titulo}
                            onChange={(e) => setFormData({ ...formData, titulo: e.target.value })}
                        />
                    </div>
                    <div className="form-group">
                        <label>Tipo de Documento</label>
                        <select
                            value={formData.tipo}
                            onChange={(e) => setFormData({ ...formData, tipo: parseInt(e.target.value) })}
                        >
                            <option value={0}>Manual de Calidad</option>
                            <option value={1}>Procedimiento</option>
                            <option value={2}>Instructivo</option>
                            <option value={3}>Formulario</option>
                            <option value={5}>Otro</option>
                        </select>
                    </div>
                    <div className="form-group">
                        <label>Área / Proceso</label>
                        <select
                            value={formData.area}
                            onChange={(e) => setFormData({ ...formData, area: parseInt(e.target.value) })}
                        >
                            <option value={0}>Dirección</option>
                            <option value={1}>Comercial</option>
                            <option value={2}>Operacional (Capacitación)</option>
                            <option value={3}>Apoyo</option>
                            <option value={4}>Administrativa</option>
                        </select>
                    </div>
                    <div className="form-group">
                        <label>Motivo del Cambio</label>
                        <textarea
                            rows="3"
                            placeholder="Explica brevemente por qué se crea o actualiza..."
                            value={formData.descripcionCambio}
                            onChange={(e) => setFormData({ ...formData, descripcionCambio: e.target.value })}
                        ></textarea>
                    </div>
                </aside>

                {/* Área de Redacción */}
                <main className="editor-area card">
                    <ReactQuill
                        theme="snow"
                        value={formData.contenidoHtml}
                        onChange={(content) => setFormData({ ...formData, contenidoHtml: content })}
                        modules={modules}
                        placeholder="Comienza a redactar el contenido de tu documento aquí..."
                    />
                </main>
            </div>

            {/* Modal de Búsqueda de Base */}
            {buscandoBase && (
                <div className="modal-overlay">
                    <div className="modal-content">
                        <header className="modal-header">
                            <h2>Seleccionar Documento Base</h2>
                            <button className="btn-close" onClick={() => setBuscandoBase(false)}>×</button>
                        </header>
                        <div className="modal-body">
                            <ul className="doc-base-list">
                                {documentos.map(doc => (
                                    <li key={doc.id} onClick={() => handleSelectBase(doc)}>
                                        <FileText size={16} />
                                        <div>
                                            <strong>{doc.codigo}</strong>
                                            <span>{doc.titulo}</span>
                                        </div>
                                    </li>
                                ))}
                            </ul>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
};

export default RedactorDocumento;
