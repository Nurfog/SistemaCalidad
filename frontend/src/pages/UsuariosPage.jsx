import { useState, useEffect } from 'react';
import { Users, UserPlus, Shield, Check, X, Search, Mail, Key } from 'lucide-react';
import api from '../api/client';
import '../styles/UsuariosPage.css';

const UsuariosPage = () => {
    const [usuariosPermitidos, setUsuariosPermitidos] = useState([]);
    const [usuariosSige, setUsuariosSige] = useState([]);
    const [loading, setLoading] = useState(true);
    const [showModal, setShowModal] = useState(false);
    const [searchTerm, setSearchTerm] = useState('');
    const [newUser, setNewUser] = useState({
        rut: '',
        rol: 'Lector',
        nombreCompleto: '',
        password: '',
        email: ''
    });

    useEffect(() => {
        fetchUsuarios();
    }, []);

    const fetchUsuarios = async () => {
        setLoading(true);
        try {
            const [permitidosRes, activosRes] = await Promise.all([
                api.get('/Usuarios'),
                api.get('/Usuarios/activos')
            ]);
            setUsuariosPermitidos(permitidosRes.data);
            setUsuariosSige(activosRes.data);
        } catch (error) {
            console.error('Error al cargar usuarios:', error);
        } finally {
            setLoading(false);
        }
    };

    const handleAsignar = async (e) => {
        e.preventDefault();
        try {
            await api.post('/Usuarios/asignar', newUser);
            alert('Usuario configurado con éxito');
            setShowModal(false);
            setNewUser({ rut: '', rol: 'Lector', nombreCompleto: '', password: '', email: '' });
            fetchUsuarios();
        } catch (error) {
            console.error('Error al asignar usuario:', error);
            alert('Error al asignar permisos');
        }
    };

    const filtrarPermitidos = usuariosPermitidos.filter(u =>
        u.nombre.toLowerCase().includes(searchTerm.toLowerCase()) ||
        u.rut.toString().includes(searchTerm)
    );

    return (
        <div className="usuarios-container">
            <header className="page-header">
                <div>
                    <h1>Administración de Usuarios</h1>
                    <p>Gestiona roles y accesos al sistema de calidad</p>
                </div>
                <button className="btn-primary" onClick={() => setShowModal(true)}>
                    <UserPlus size={20} />
                    <span>Configurar Acceso</span>
                </button>
            </header>

            <div className="search-bar-container">
                <Search size={20} />
                <input
                    type="text"
                    placeholder="Buscar por nombre o RUT..."
                    value={searchTerm}
                    onChange={(e) => setSearchTerm(e.target.value)}
                />
            </div>

            <div className="users-grid">
                {loading ? (
                    <div className="loading-spinner">Cargando usuarios...</div>
                ) : (
                    <table className="custom-table">
                        <thead>
                            <tr>
                                <th>Nombre</th>
                                <th>RUT / ID</th>
                                <th>Rol</th>
                                <th>Tipo</th>
                                <th>Estado</th>
                                <th>Acciones</th>
                            </tr>
                        </thead>
                        <tbody>
                            {filtrarPermitidos.map(u => (
                                <tr key={u.rut}>
                                    <td>{u.nombre}</td>
                                    <td>{u.rut}</td>
                                    <td>
                                        <span className={`role-badge ${u.rol.toLowerCase()}`}>
                                            {u.rol}
                                        </span>
                                    </td>
                                    <td>{u.esLocal ? 'Local (Externo)' : 'SIGE'}</td>
                                    <td>
                                        <span className={`status-dot ${u.activo ? 'active' : 'inactive'}`}></span>
                                        {u.activo ? 'Activo' : 'Inactivo'}
                                    </td>
                                    <td>
                                        <button className="btn-icon" title="Editar Rol">
                                            <Shield size={18} />
                                        </button>
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                )}
            </div>

            {showModal && (
                <div className="modal-overlay">
                    <div className="modal-content user-modal">
                        <header className="modal-header">
                            <h2>Configurar Nuevo Acceso</h2>
                            <button onClick={() => setShowModal(false)}><X size={24} /></button>
                        </header>

                        <form onSubmit={handleAsignar}>
                            <div className="form-group">
                                <label>Seleccionar Usuario (desde SIGE)</label>
                                <select
                                    value={newUser.rut}
                                    onChange={(e) => {
                                        const selected = usuariosSige.find(u => u.id === e.target.value);
                                        setNewUser({ ...newUser, rut: e.target.value, nombreCompleto: selected?.nombreCompleto || '' });
                                    }}
                                    disabled={newUser.rol === 'AuditorExterno' && newUser.password}
                                >
                                    <option value="">-- Seleccione un usuario --</option>
                                    {usuariosSige.map(u => (
                                        <option key={u.id} value={u.id}>{u.nombreCompleto} ({u.id})</option>
                                    ))}
                                </select>
                                <p className="help-text">Si es un Auditor Externo nuevo, ingresa su RUT/ID manualmente abajo.</p>
                            </div>

                            <div className="form-grid">
                                <div className="form-group">
                                    <label>RUT / Identificador</label>
                                    <input
                                        type="text"
                                        value={newUser.rut}
                                        onChange={(e) => setNewUser({ ...newUser, rut: e.target.value })}
                                        required
                                    />
                                </div>
                                <div className="form-group">
                                    <label>Rol Asignado</label>
                                    <select
                                        value={newUser.rol}
                                        onChange={(e) => setNewUser({ ...newUser, rol: e.target.value })}
                                    >
                                        <option value="Lector">Lector (Solo ver)</option>
                                        <option value="Responsable">Responsable de Proceso</option>
                                        <option value="AuditorInterno">Auditor Interno</option>
                                        <option value="AuditorExterno">Auditor Externo</option>
                                        <option value="Administrador">Administrador</option>
                                    </select>
                                </div>
                            </div>

                            {newUser.rol === 'AuditorExterno' && (
                                <div className="external-auth-section">
                                    <h3>Configuración de Cuenta Local (Auditores Externos)</h3>
                                    <div className="form-group">
                                        <label>Nombre Completo</label>
                                        <input
                                            type="text"
                                            value={newUser.nombreCompleto}
                                            onChange={(e) => setNewUser({ ...newUser, nombreCompleto: e.target.value })}
                                        />
                                    </div>
                                    <div className="form-grid">
                                        <div className="form-group">
                                            <label><Mail size={14} /> Correo</label>
                                            <input
                                                type="email"
                                                value={newUser.email}
                                                onChange={(e) => setNewUser({ ...newUser, email: e.target.value })}
                                            />
                                        </div>
                                        <div className="form-group">
                                            <label><Key size={14} /> Contraseña</label>
                                            <input
                                                type="password"
                                                placeholder="Solo si es cuenta nueva"
                                                value={newUser.password}
                                                onChange={(e) => setNewUser({ ...newUser, password: e.target.value })}
                                            />
                                        </div>
                                    </div>
                                </div>
                            )}

                            <footer className="modal-footer">
                                <button type="button" className="btn-secondary" onClick={() => setShowModal(false)}>Cancelar</button>
                                <button type="submit" className="btn-primary">Configurar Acceso</button>
                            </footer>
                        </form>
                    </div>
                </div>
            )}
        </div>
    );
};

export default UsuariosPage;
