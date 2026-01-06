import { Outlet, NavLink, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import {
    LayoutDashboard,
    Files,
    FileCheck,
    AlertTriangle,
    History,
    LogOut,
    User,
    ChevronRight,
    ShieldAlert,
    FileText
} from 'lucide-react';
import '../styles/Layout.css';

const Layout = () => {
    const { user, logout } = useAuth();
    const navigate = useNavigate();

    const menuItems = [
        { name: 'Dashboard', path: '/', icon: <LayoutDashboard size={20} /> },
        { name: 'Documentos', path: '/documentos', icon: <Files size={20} /> },
        { name: 'Registros', path: '/registros', icon: <FileCheck size={20} /> },
        { name: 'No Conformidades', path: '/no-conformidades', icon: <ShieldAlert size={20} /> },
        { name: 'Anexos y Plantillas', path: '/anexos', icon: <FileText size={20} /> },
        { name: 'Auditoría', path: '/auditoria', icon: <History size={20} /> },
        ...(user?.Rol === 'Administrador' || user?.Rol === 'AuditorExterno' || user?.Rol === 'AuditorInterno' ? [{ name: 'Trazabilidad', path: '/trazabilidad', icon: <History size={20} /> }] : []),
        ...(user?.Rol === 'Administrador' ? [{ name: 'Usuarios', path: '/usuarios', icon: <User size={20} /> }] : []),
    ];

    return (
        <div className="app-layout">
            {/* Sidebar */}
            <aside className="sidebar">
                <div className="sidebar-header">
                    <div className="brand">
                        <div className="brand-dot"></div>
                        <span>SGC Calidad</span>
                    </div>
                </div>

                <nav className="sidebar-nav">
                    {menuItems.map((item) => (
                        <NavLink
                            key={item.path}
                            to={item.path}
                            className={({ isActive }) => isActive ? 'nav-item active' : 'nav-item'}
                        >
                            <span className="nav-icon">{item.icon}</span>
                            <span className="nav-label">{item.name}</span>
                            <ChevronRight className="nav-arrow" size={14} />
                        </NavLink>
                    ))}
                </nav>

                <div className="sidebar-footer">
                    <button onClick={logout} className="logout-button">
                        <LogOut size={20} />
                        <span>Cerrar Sesión</span>
                    </button>
                </div>
            </aside>

            {/* Main Content Area */}
            <main className="main-container">
                {/* Header / Navbar */}
                <header className="navbar">
                    <div className="navbar-left">
                        <h2 className="page-title">Sistema de Gestión de Calidad</h2>
                    </div>

                    <div className="navbar-right">
                        <div className="user-profile">
                            <div className="user-info">
                                <span className="user-name">{user?.Nombre || 'Usuario'}</span>
                                <span className="user-role">{user?.Rol || 'Colaborador'}</span>
                            </div>
                            <div className="user-avatar">
                                <User size={20} />
                            </div>
                        </div>
                    </div>
                </header>

                {/* Content Outlet */}
                <div className="content-outlet">
                    <Outlet />
                </div>
            </main>
        </div>
    );
};

export default Layout;
