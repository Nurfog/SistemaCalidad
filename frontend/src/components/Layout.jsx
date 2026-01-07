import { Outlet, NavLink, useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { motion, AnimatePresence } from 'framer-motion';
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
    FileText,
    Sun,
    Moon
} from 'lucide-react';
import { useTheme } from '../context/ThemeContext';
import '../styles/Layout.css';

const Layout = () => {
    const { user, logout } = useAuth();
    const { isDarkMode, toggleTheme } = useTheme();
    const navigate = useNavigate();
    const location = useLocation();

    const menuItems = [
        { name: 'Dashboard', path: '/', icon: <LayoutDashboard size={20} /> },
        { name: 'Documentos', path: '/documentos', icon: <Files size={20} /> },
        { name: 'Registros', path: '/registros', icon: <FileCheck size={20} /> },
        { name: 'No Conformidades', path: '/no-conformidades', icon: <ShieldAlert size={20} /> },
        { name: 'Anexos y Plantillas', path: '/anexos', icon: <FileText size={20} /> },
        ...(user?.Rol === 'Administrador' || user?.Rol === 'AuditorExterno' || user?.Rol === 'AuditorInterno' ? [{ name: 'Auditoría', path: '/auditoria', icon: <History size={20} /> }] : []),
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
                        <button className="theme-toggle" onClick={toggleTheme}>
                            {isDarkMode ? <Sun size={20} /> : <Moon size={20} />}
                        </button>
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
                    <AnimatePresence mode="wait">
                        <motion.div
                            key={location.pathname}
                            initial={{ opacity: 0, y: 10 }}
                            animate={{ opacity: 1, y: 0 }}
                            exit={{ opacity: 0, y: -10 }}
                            transition={{ duration: 0.2 }}
                        >
                            <Outlet />
                        </motion.div>
                    </AnimatePresence>
                </div>
            </main>
        </div>
    );
};

export default Layout;
