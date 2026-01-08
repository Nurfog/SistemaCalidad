import { useState, useEffect } from 'react';
import { X, Folder, ChevronRight, Home, CheckCircle2 } from 'lucide-react';
import api from '../api/client';
import '../styles/DocumentModal.css'; // Reutilizamos estilos base de modales

const MoveModal = ({ isOpen, onClose, docId, docTitulo, onSave }) => {
    const [carpetas, setCarpetas] = useState([]);
    const [loading, setLoading] = useState(true);
    const [targetCarpetaId, setTargetCarpetaId] = useState(null);
    const [breadcrumb, setBreadcrumb] = useState([]);

    useEffect(() => {
        if (isOpen) {
            fetchCarpetas();
        }
    }, [isOpen]);

    const fetchCarpetas = async (parentId = null) => {
        setLoading(true);
        try {
            const response = await api.get('/CarpetasDocumentos', { params: { parentId } });
            setCarpetas(response.data);
        } catch (error) {
            console.error('Error al cargar carpetas:', error);
        } finally {
            setLoading(false);
        }
    };

    const handleNavigate = (carpeta) => {
        setBreadcrumb([...breadcrumb, carpeta]);
        setTargetCarpetaId(carpeta.id);
        fetchCarpetas(carpeta.id);
    };

    const handleBack = (index) => {
        if (index === -1) {
            setBreadcrumb([]);
            setTargetCarpetaId(null);
            fetchCarpetas(null);
        } else {
            const newBreadcrumb = breadcrumb.slice(0, index + 1);
            setBreadcrumb(newBreadcrumb);
            const parent = newBreadcrumb[newBreadcrumb.length - 1];
            setTargetCarpetaId(parent.id);
            fetchCarpetas(parent.id);
        }
    };

    const handleMove = async () => {
        try {
            await api.put(`/Documentos/${docId}/mover`, { carpetaId: targetCarpetaId });
            onSave();
            onClose();
        } catch (error) {
            alert('Error al mover el documento: ' + (error.response?.data?.mensaje || error.message));
        }
    };

    if (!isOpen) return null;

    return (
        <div className="modal-overlay">
            <div className="modal-content card" style={{ maxWidth: '500px' }}>
                <header className="modal-header">
                    <div>
                        <h2>Mover Documento</h2>
                        <p className="text-muted" style={{ fontSize: '0.85rem' }}>{docTitulo}</p>
                    </div>
                    <button onClick={onClose} className="close-btn"><X size={20} /></button>
                </header>

                <div className="modal-body" style={{ padding: '20px' }}>
                    <div className="move-breadcrumb" style={{ display: 'flex', gap: '8px', alignItems: 'center', marginBottom: '15px', fontSize: '0.9rem' }}>
                        <span
                            onClick={() => handleBack(-1)}
                            style={{ cursor: 'pointer', color: targetCarpetaId === null ? 'var(--primary)' : 'inherit', fontWeight: targetCarpetaId === null ? '600' : 'normal' }}
                        >
                            <Home size={16} /> Raíz
                        </span>
                        {breadcrumb.map((c, i) => (
                            <div key={c.id} style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                                <ChevronRight size={14} />
                                <span
                                    onClick={() => handleBack(i)}
                                    style={{ cursor: 'pointer', color: targetCarpetaId === c.id ? 'var(--primary)' : 'inherit', fontWeight: targetCarpetaId === c.id ? '600' : 'normal' }}
                                >
                                    {c.nombre}
                                </span>
                            </div>
                        ))}
                    </div>

                    <div className="folder-list" style={{ maxHeight: '300px', overflowY: 'auto', border: '1px solid var(--border-color)', borderRadius: '12px' }}>
                        {loading ? (
                            <div style={{ padding: '20px', textAlign: 'center' }}>Cargando carpetas...</div>
                        ) : carpetas.length > 0 ? (
                            carpetas.map(c => (
                                <div
                                    key={c.id}
                                    className="folder-item"
                                    onClick={() => handleNavigate(c)}
                                    style={{
                                        padding: '12px 16px',
                                        display: 'flex',
                                        alignItems: 'center',
                                        justifyContent: 'space-between',
                                        cursor: 'pointer',
                                        borderBottom: '1px solid var(--border-color)',
                                        transition: 'background 0.2s'
                                    }}
                                    onMouseEnter={(e) => e.currentTarget.style.background = 'rgba(56, 189, 248, 0.05)'}
                                    onMouseLeave={(e) => e.currentTarget.style.background = 'transparent'}
                                >
                                    <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
                                        <Folder size={18} fill={c.color || '#3b82f6'} fillOpacity={0.2} style={{ color: c.color || '#3b82f6' }} />
                                        <span>{c.nombre}</span>
                                    </div>
                                    <ChevronRight size={16} className="text-muted" />
                                </div>
                            ))
                        ) : (
                            <div style={{ padding: '20px', textAlign: 'center', color: 'var(--text-secondary)' }}>
                                No hay más subcarpetas aquí.
                            </div>
                        )}
                    </div>
                </div>

                <footer className="modal-footer">
                    <div style={{ fontSize: '0.85rem', color: 'var(--text-secondary)' }}>
                        Se moverá a: <strong>{targetCarpetaId ? breadcrumb[breadcrumb.length - 1]?.nombre : 'Raíz'}</strong>
                    </div>
                    <div style={{ display: 'flex', gap: '12px' }}>
                        <button onClick={onClose} className="btn-secondary">Cancelar</button>
                        <button onClick={handleMove} className="btn-primary">
                            <CheckCircle2 size={18} />
                            Mover Aquí
                        </button>
                    </div>
                </footer>
            </div>
        </div>
    );
};

export default MoveModal;
