import { useState, useEffect } from 'react';
import api from '../api/client';
import { useAuth } from '../context/AuthContext';
import {
    Search,
    Plus,
    FileCheck,
    Download,
    Calendar,
    BadgeCheck,
    HardDrive,
    Trash2,
    FolderPlus // Nuevo icono importado
} from 'lucide-react';
import RegistroModal from '../components/RegistroModal';
import CarpetaModal from '../components/CarpetaModal';
import '../styles/Registros.css';

const Registros = () => {
    const { user } = useAuth();
    const [registros, setRegistros] = useState([]);
    const [carpetas, setCarpetas] = useState([]);
    const [loading, setLoading] = useState(true);
    const [buscar, setBuscar] = useState('');

    // Estados para Modales y Navegación
    const [isModalOpen, setIsModalOpen] = useState(false);
    const [isCarpetaModalOpen, setIsCarpetaModalOpen] = useState(false);
    const [carpetaActual, setCarpetaActual] = useState(null); // null = Raíz

    const fetchData = async () => {
        setLoading(true);
        try {
            const params = {
                buscar: buscar || undefined,
                carpetaId: carpetaActual?.id || null
            };

            // Cargar registros filtrados
            const resRegistros = await api.get('/Registros', { params });
            setRegistros(resRegistros.data);

            // Cargar carpetas (Solo si estamos en la raíz y no estamos buscando)
            // Nota: Si quieres carpetas anidadas, el backend debería soportarlo. 
            // Por ahora asumimos carpetas solo en raíz o filtramos en frontend.
            if (!carpetaActual && !buscar) {
                const resCarpetas = await api.get('/Carpetas');
                setCarpetas(resCarpetas.data);
            } else {
                setCarpetas([]); // Ocultar carpetas si estamos dentro de una o buscando
            }
        } catch (error) {
            console.error('Error al cargar datos:', error);
        } finally {
            setLoading(false);
        }
    };

    const handleSaveRegistro = async (formData) => {
        try {
            // Adjuntar carpeta actual si existe
            if (carpetaActual) {
                formData.append('carpetaId', carpetaActual.id);
            }

            await api.post('/Registros', formData, {
                headers: { 'Content-Type': 'multipart/form-data' }
            });
            fetchData();
        } catch (error) {
            console.error('Error al guardar registro:', error);
            throw error;
        }
    };

    const handleCreateCarpeta = async (data) => {
        try {
            await api.post('/Carpetas', data);
            fetchData();
        } catch (error) {
            console.error('Error al crear carpeta:', error);
            alert('Error al crear la carpeta');
        }
    };

    useEffect(() => {
        fetchData();
    }, [buscar, carpetaActual]);

    const handleDownload = async (regId, nombreArchivo) => {
        try {
            const response = await api.get(`/Registros/${regId}/descargar`, {
                responseType: 'blob'
            });
            const url = window.URL.createObjectURL(new Blob([response.data]));
            const link = document.createElement('a');
            link.href = url;
            link.setAttribute('download', nombreArchivo || `registro_${regId}.pdf`);
            document.body.appendChild(link);
            link.click();
            link.remove();
        } catch (error) {
            alert('Error al descargar el registro.');
        }
    };

    const handleDeleteCarpeta = async (e, id) => {
        e.stopPropagation();
        if (!confirm('¿Estás seguro? La carpeta debe estar vacía para eliminarse.')) return;
        try {
            await api.delete(`/Carpetas/${id}`);
            fetchData();
        } catch (error) {
            alert(error.response?.data || 'Error al eliminar carpeta');
        }
    };

    return (
        <div className="registros-page">
            <header className="page-header">
                <div className="header-left">
                    <div className="breadcrumbs">
                        <h1 onClick={() => setCarpetaActual(null)} style={{ cursor: 'pointer', opacity: carpetaActual ? 0.5 : 1 }}>
                            Registros de Calidad
                        </h1>
                        {carpetaActual && (
                            <>
                                <span>/</span>
                                <h1>{carpetaActual.nombre}</h1>
                            </>
                        )}
                    </div>
                    <p>Evidencias y control documental</p>
                </div>
                {(user?.Rol?.includes('Administrador') || user?.Rol?.includes('Escritor')) && (
                    <div className="header-actions" style={{ display: 'flex', gap: '1rem' }}>
                        {!carpetaActual && (
                            <button className="btn-secondary" onClick={() => setIsCarpetaModalOpen(true)}>
                                <FolderPlus size={20} />
                                <span>Nueva Carpeta</span>
                            </button>
                        )}
                        <button className="btn-primary" onClick={() => setIsModalOpen(true)}>
                            <Plus size={20} />
                            <span>Nuevo Registro</span>
                        </button>
                    </div>
                )}
            </header>

            <section className="toolbar">
                <div className="search-box">
                    <Search size={20} className="search-icon" />
                    <input
                        type="text"
                        placeholder="Buscar evidencias..."
                        value={buscar}
                        onChange={(e) => setBuscar(e.target.value)}
                    />
                </div>
            </section>

            {/* Vista de Carpetas (Solo en raíz) */}
            {!carpetaActual && !buscar && carpetas.length > 0 && (
                <div className="carpetas-grid">
                    {carpetas.map(carpeta => (
                        <div
                            key={carpeta.id}
                            className="carpeta-card"
                            onClick={() => setCarpetaActual(carpeta)}
                            style={{ borderColor: carpeta.color }} // Borde sutil del color
                        >
                            <div className="carpeta-icon" style={{ color: carpeta.color }}>
                                <FolderPlus size={32} />
                            </div>
                            <span className="carpeta-name">{carpeta.nombre}</span>
                            {user?.Rol === 'Administrador' && (
                                <button className="btn-delete-folder" onClick={(e) => handleDeleteCarpeta(e, carpeta.id)}>
                                    <Trash2 size={14} />
                                </button>
                            )}
                        </div>
                    ))}
                </div>
            )}

            <div className="table-container card">
                {loading ? (
                    <div className="table-loading">Cargando contenido...</div>
                ) : (
                    <table className="master-table">
                        <thead>
                            <tr>
                                <th>Identificador</th>
                                <th>Nombre del Registro</th>
                                <th>Almacenamiento</th>
                                <th>Retención</th>
                                <th>Fecha</th>
                                <th className="actions-col">Acciones</th>
                            </tr>
                        </thead>
                        <tbody>
                            {registros.length > 0 ? registros.map((reg) => (
                                <tr key={reg.id}>
                                    <td className="col-id"><strong>{reg.identificador}</strong></td>
                                    <td className="col-name">
                                        <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                                            <FileCheck size={16} className="text-muted" />
                                            {reg.nombre}
                                        </div>
                                    </td>
                                    <td className="col-storage">
                                        <div className="storage-info">
                                            <HardDrive size={14} />
                                            <span>Digital</span>
                                        </div>
                                    </td>
                                    <td className="col-retention">
                                        <span className="retention-pill">{reg.anosRetencion} años</span>
                                    </td>
                                    <td className="col-date">
                                        {new Date(reg.fechaAlmacenamiento).toLocaleDateString()}
                                    </td>
                                    <td className="col-actions">
                                        <div className="action-buttons">
                                            <button
                                                className="action-btn"
                                                title="Descargar"
                                                onClick={() => {
                                                    const fileName = reg.rutaArchivo?.split('_').slice(1).join('_') || reg.nombre;
                                                    handleDownload(reg.id, fileName);
                                                }}
                                            >
                                                <Download size={18} />
                                            </button>
                                            {user?.Rol === 'Administrador' && (
                                                <button className="action-btn text-error" title="Eliminar">
                                                    <Trash2 size={18} />
                                                </button>
                                            )}
                                        </div>
                                    </td>
                                </tr>
                            )) : (
                                <tr>
                                    <td colSpan="6" className="empty-row">
                                        {carpetaActual ? 'Esta carpeta está vacía.' : 'No hay registros en la raíz.'}
                                    </td>
                                </tr>
                            )}
                        </tbody>
                    </table>
                )}
            </div>

            <RegistroModal
                isOpen={isModalOpen}
                onClose={() => setIsModalOpen(false)}
                onSave={handleSaveRegistro}
            />

            <CarpetaModal
                isOpen={isCarpetaModalOpen}
                onClose={() => setIsCarpetaModalOpen(false)}
                onSave={handleCreateCarpeta}
            />
        </div>
    );
};

export default Registros;
