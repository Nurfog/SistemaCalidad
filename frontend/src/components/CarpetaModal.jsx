import { useState } from 'react';
import { X, Save, FolderPlus, Palette } from 'lucide-react';
import '../styles/NCModal.css'; // Reutilizamos estilos base

const CarpetaModal = ({ isOpen, onClose, onSave }) => {
    const [nombre, setNombre] = useState('');
    const [color, setColor] = useState('#38bdf8');
    const [loading, setLoading] = useState(false);

    if (!isOpen) return null;

    const handleSubmit = async (e) => {
        e.preventDefault();
        setLoading(true);
        try {
            await onSave({ nombre, color });
            setNombre('');
            setColor('#38bdf8');
            onClose();
        } catch (error) {
            console.error(error);
        } finally {
            setLoading(false);
        }
    };

    const colores = [
        '#38bdf8', // Cyan (Default)
        '#f87171', // Red
        '#fbbf24', // Amber
        '#34d399', // Emerald
        '#818cf8', // Indigo
        '#c084fc', // Purple
        '#fb7185'  // Rose
    ];

    return (
        <div className="modal-overlay">
            <div className="modal-content nc-modal" style={{ maxWidth: '450px' }}>
                <header className="modal-header">
                    <div className="header-title">
                        <div className="icon-badge highlight">
                            <FolderPlus size={20} />
                        </div>
                        <div>
                            <h2>Nueva Carpeta</h2>
                            <p>Organiza tus registros</p>
                        </div>
                    </div>
                    <button className="close-btn" onClick={onClose}>
                        <X size={24} />
                    </button>
                </header>

                <form onSubmit={handleSubmit} className="modal-form">
                    <div className="form-group">
                        <label>Nombre de la Carpeta</label>
                        <input
                            autoFocus
                            type="text"
                            required
                            placeholder="Ej: Registros 2024"
                            value={nombre}
                            onChange={(e) => setNombre(e.target.value)}
                        />
                    </div>

                    <div className="form-group">
                        <label>Etiqueta de Color</label>
                        <div style={{ display: 'flex', gap: '0.5rem', marginTop: '0.5rem' }}>
                            {colores.map(c => (
                                <button
                                    key={c}
                                    type="button"
                                    onClick={() => setColor(c)}
                                    style={{
                                        width: '24px',
                                        height: '24px',
                                        borderRadius: '50%',
                                        background: c,
                                        border: color === c ? '2px solid white' : 'none',
                                        cursor: 'pointer',
                                        boxShadow: color === c ? '0 0 0 2px var(--primary-color)' : 'none',
                                        transition: 'all 0.2s'
                                    }}
                                />
                            ))}
                        </div>
                    </div>

                    <footer className="modal-footer">
                        <button type="button" className="btn-secondary" onClick={onClose}>
                            Cancelar
                        </button>
                        <button type="submit" className="btn-primary" disabled={loading}>
                            <Save size={18} />
                            <span>Crear Carpeta</span>
                        </button>
                    </footer>
                </form>
            </div>
        </div>
    );
};

export default CarpetaModal;
