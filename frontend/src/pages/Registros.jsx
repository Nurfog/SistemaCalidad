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
    Trash2
} from 'lucide-react';
import RegistroModal from '../components/RegistroModal';
import '../styles/Registros.css';

const Registros = () => {
    const { user } = useAuth();
    const [registros, setRegistros] = useState([]);
    const [loading, setLoading] = useState(true);
    const [buscar, setBuscar] = useState('');
    const [isModalOpen, setIsModalOpen] = useState(false);

    const fetchRegistros = async () => {
        setLoading(true);
        try {
            const params = { buscar: buscar || undefined };
            const response = await api.get('/Registros', { params });
            setRegistros(response.data);
        } catch (error) {
            console.error('Error al cargar registros:', error);
        } finally {
            setLoading(false);
        }
    };

    const handleSaveRegistro = async (formData) => {
        try {
            await api.post('/Registros', formData, {
                headers: { 'Content-Type': 'multipart/form-data' }
            });
            fetchRegistros();
        } catch (error) {
            console.error('Error al guardar registro:', error);
            throw error;
        }
    };

    useEffect(() => {
        const timer = setTimeout(() => {
            fetchRegistros();
        }, 500);
        return () => clearTimeout(timer);
    }, [buscar]);

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

    return (
        <div className="registros-page">
            <header className="page-header">
                <div className="header-left">
                    <h1>Registros de Calidad</h1>
                    <p>Evidencias y control de retención de registros NCh 2728</p>
                </div>
                {(user?.Rol === 'Administrador' || user?.Rol === 'Escritor') && (
                    <button className="btn-primary" onClick={() => setIsModalOpen(true)}>
                        <Plus size={20} />
                        <span>Nuevo Registro</span>
                    </button>
                )}
            </header>

            <section className="toolbar">
                <div className="search-box">
                    <Search size={20} className="search-icon" />
                    <input
                        type="text"
                        placeholder="Buscar por identificador o nombre..."
                        value={buscar}
                        onChange={(e) => setBuscar(e.target.value)}
                    />
                </div>
            </section>

            <div className="table-container card">
                {loading ? (
                    <div className="table-loading">Cargando evidencias...</div>
                ) : (
                    <table className="master-table">
                        <thead>
                            <tr>
                                <th>Identificador</th>
                                <th>Nombre del Registro</th>
                                <th>Almacenamiento</th>
                                <th>Retención (Años)</th>
                                <th>Fecha</th>
                                <th className="actions-col">Acciones</th>
                            </tr>
                        </thead>
                        <tbody>
                            {registros.length > 0 ? registros.map((reg) => (
                                <tr key={reg.id}>
                                    <td className="col-id"><strong>{reg.identificador}</strong></td>
                                    <td className="col-name">{reg.nombre}</td>
                                    <td className="col-storage">
                                        <div className="storage-info">
                                            <HardDrive size={14} />
                                            <span>Nube S3 / Digital</span>
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
                                                onClick={() => handleDownload(reg.id, reg.nombre + '.pdf')}
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
                                    <td colSpan="6" className="empty-row">No se han encontrado registros de calidad archivados.</td>
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
        </div>
    );
};

export default Registros;
