import { Routes, Route, Navigate } from 'react-router-dom';
import { useAuth } from './context/AuthContext';
import Login from './pages/Login';
import Dashboard from './pages/Dashboard';
import Documentos from './pages/Documentos';
import NoConformidades from './pages/NoConformidades';
import Registros from './pages/Registros';
import UsuariosPage from './pages/UsuariosPage';
import PanelAuditoriaExterna from './pages/PanelAuditoriaExterna';
import Anexos from './pages/Anexos';
import RedactorDocumento from './pages/RedactorDocumento';
import Layout from './components/Layout';
import NotificationToast from './components/NotificationToast';

// Componente para proteger rutas
const PrivateRoute = ({ children }) => {
  const { user, loading } = useAuth();

  if (loading) return <div>Cargando...</div>;
  if (!user) return <Navigate to="/login" />;

  return children;
};

function App() {
  return (
    <>
      <Routes>
        <Route path="/login" element={<Login />} />

        <Route path="/" element={
          <PrivateRoute>
            <Layout />
          </PrivateRoute>
        }>
          <Route index element={<Dashboard />} />
          <Route path="documentos" element={<Documentos />} />
          <Route path="no-conformidades" element={<NoConformidades />} />
          <Route path="registros" element={<Registros />} />
          <Route path="usuarios" element={<UsuariosPage />} />
          <Route path="auditoria" element={<PanelAuditoriaExterna />} />
          <Route path="anexos" element={<Anexos />} />
          <Route path="redactar/:baseId?" element={<RedactorDocumento />} />
          {/* Futuras rutas: Auditoria, NoConformidades, etc. */}
        </Route>

        <Route path="*" element={<Navigate to="/" />} />
      </Routes>
      <NotificationToast />
    </>
  );
}

export default App;
