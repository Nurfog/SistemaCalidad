import { useState, useEffect } from 'react';
import ReactQuill from 'react-quill-new';
import 'react-quill-new/dist/quill.snow.css';
import { Save, FileText, ArrowLeft, Loader2, Search, Plus } from 'lucide-react';
import api from '../api/client';
import { useNavigate, useParams } from 'react-router-dom';
import { parseEditorCommands, generateTableHtml } from '../utils/EditorUtils';
import '../styles/RedactorDocumento.css';

const RedactorDocumento = () => {
    const navigate = useNavigate();
    const { baseId } = useParams();
    const [loading, setLoading] = useState(false);
    const [buscandoBase, setBuscandoBase] = useState(false);
    const [extrayendoConIA, setExtrayendoConIA] = useState(false);
    const [documentos, setDocumentos] = useState([]);

    const [formData, setFormData] = useState({
        id: null,
        titulo: '',
        codigo: '',
        tipo: 1, // Procedimiento por defecto
        area: 2, // Operacional por defecto
        contenidoHtml: '',
        descripcionCambio: '',
        encabezadoAdicional: '',
        piePaginaPersonalizado: ''
    });

    const sgcBlocks = [
        { name: 'Objetivo', content: '<h2>1. OBJETIVO</h2><p>Establecer las directrices para...</p>' },
        { name: 'Alcance', content: '<h2>2. ALCANCE</h2><p>Este procedimiento aplica a todos los procesos de...</p>' },
        { name: 'Responsabilidades', content: '<h2>3. RESPONSABILIDADES</h2><p><strong>Gerencia:</strong> Supervisar...<br><strong>Encargado:</strong> Ejecutar...</p>' },
        { name: 'Definiciones', content: '<h2>4. DEFINICIONES</h2><ul><li><strong>SGC:</strong> Sistema de Gestión de Calidad.</li></ul>' },
        { name: 'Desarrollo', content: '<h2>5. DESARROLLO DEL PROCESO</h2><p>El proceso inicia cuando...</p>' },
        { name: 'Documentos Relacionados', content: '<h2>6. DOCUMENTOS RELACIONADOS</h2><ul><li>Manual de Calidad</li><li>Listado Maestro</li></ul>' },
        { name: 'Anexos', content: '<h2>7. ANEXOS</h2><p>No aplica / Ver Listado de Anexos</p>' }
    ];

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

    const handleSelectBase = async (doc) => {
        setBuscandoBase(false);
        setExtrayendoConIA(true);

        try {
            // Intentamos extraer el contenido con IA
            const res = await api.get(`/Documentos/${doc.id}/extraer-contenido`);
            const { contenidoHtml } = res.data;

            setFormData({
                ...formData,
                id: doc.id,
                titulo: doc.titulo,
                codigo: doc.codigo,
                tipo: doc.tipo,
                area: doc.area,
                descripcionCambio: `Actualización basada en ${doc.codigo}`,
                encabezadoAdicional: doc.encabezadoAdicional || '',
                piePaginaPersonalizado: doc.piePaginaPersonalizado || '',
                contenidoHtml: contenidoHtml || '<p></p>'
            });
        } catch (error) {
            console.error('Error al extraer contenido con IA:', error);
            // Si falla la IA, al menos cargamos los metadatos
            setFormData({
                ...formData,
                id: doc.id,
                titulo: doc.titulo,
                codigo: doc.codigo,
                tipo: doc.tipo,
                area: doc.area,
            });
            alert('No pudimos extraer el contenido exacto con IA, pero hemos cargado los metadatos del documento.');
        } finally {
            setExtrayendoConIA(false);
        }
    };

    const handleInsertBlock = (block) => {
        setFormData(prev => ({
            ...prev,
            contenidoHtml: prev.contenidoHtml + block.content
        }));
    };

    const handleEditorChange = (content) => {
        const { content: parsedContent, hasChanged } = parseEditorCommands(content);
        setFormData(prev => ({ ...prev, contenidoHtml: parsedContent }));
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
                            rows="2"
                            placeholder="Explica brevemente por qué se crea o actualiza..."
                            value={formData.descripcionCambio}
                            onChange={(e) => setFormData({ ...formData, descripcionCambio: e.target.value })}
                        ></textarea>
                    </div>
                    <div className="form-group divider">
                        <h3>Personalización Visual</h3>
                    </div>
                    <div className="form-group">
                        <label>Texto de Encabezado (Opcional)</label>
                        <input
                            type="text"
                            placeholder="Ej: Documento de Uso Exclusivo..."
                            value={formData.encabezadoAdicional}
                            onChange={(e) => setFormData({ ...formData, encabezadoAdicional: e.target.value })}
                        />
                    </div>
                    <div className="form-group">
                        <label>Texto de Pie de Página (Opcional)</label>
                        <input
                            type="text"
                            placeholder="Ej: Impreso el {fecha} por {usuario}..."
                            value={formData.piePaginaPersonalizado}
                            onChange={(e) => setFormData({ ...formData, piePaginaPersonalizado: e.target.value })}
                        />
                    </div>
                </aside>

                {/* Área de Redacción */}
                <main className="editor-area card">
                    {extrayendoConIA && (
                        <div className="ia-extraction-loader">
                            <Loader2 size={32} className="animate-spin text-primary" />
                            <p>La IA está analizando el documento base y extrayendo el contenido...</p>
                        </div>
                    )}
                    <div className="editor-workspace">
                        <div className="quill-wrapper">
                            <ReactQuill
                                theme="snow"
                                value={formData.contenidoHtml}
                                onChange={handleEditorChange}
                                modules={modules}
                                placeholder="Comienza a redactar... Tip: Escribe tabla(3,2) para insertar una tabla rápidamente."
                            />
                        </div>

                        {/* Sidebar de Bloques Estándar */}
                        <aside className="blocks-sidebar">
                            <h4>Bloques SGC</h4>
                            <p className="text-xs text-secondary mb-3">Haz clic para insertar:</p>
                            <div className="blocks-list">
                                {sgcBlocks.map((block, idx) => (
                                    <button
                                        key={idx}
                                        className="block-btn"
                                        onClick={() => handleInsertBlock(block)}
                                    >
                                        <Plus size={14} />
                                        <span>{block.name}</span>
                                    </button>
                                ))}
                            </div>

                            <div className="helper-box mt-6">
                                <h5>Comandos Rápidos</h5>
                                <code>tabla(filas, cols)</code>
                                <code>h2(texto)</code>
                            </div>
                        </aside>
                    </div>
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
