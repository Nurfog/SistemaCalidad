import { useState } from 'react';
import { useAuth } from '../context/AuthContext';
import { useNavigate } from 'react-router-dom';
import { LogIn, ShieldCheck } from 'lucide-react';
import '../styles/Login.css';

const Login = () => {
    const [usuario, setUsuario] = useState('');
    const [password, setPassword] = useState('');
    const [error, setError] = useState('');
    const [loading, setLoading] = useState(false);

    const { login } = useAuth();
    const navigate = useNavigate();

    const handleSubmit = async (e) => {
        e.preventDefault();
        setError('');
        setLoading(true);

        const result = await login(usuario, password);

        if (result.success) {
            navigate('/');
        } else {
            setError(result.message);
            setLoading(false);
        }
    };

    return (
        <div className="login-container">
            <div className="login-card">
                <header>
                    <div className="logo-container">
                        <img src="/logo.png" alt="Logo" className="logo-image" />
                    </div>
                    <h1>NCh 2728:2015</h1>
                    <p className="subtitle">Gestión de Calidad e Innovación</p>
                </header>

                <form onSubmit={handleSubmit}>
                    {error && <div className="error-message">{error}</div>}

                    <div className="input-group">
                        <label htmlFor="usuario">Usuario (RUT o Legajo)</label>
                        <input
                            type="text"
                            id="usuario"
                            value={usuario}
                            onChange={(e) => setUsuario(e.target.value)}
                            placeholder="Ingrese su usuario"
                            required
                        />
                    </div>

                    <div className="input-group">
                        <label htmlFor="password">Contraseña</label>
                        <input
                            type="password"
                            id="password"
                            value={password}
                            onChange={(e) => setPassword(e.target.value)}
                            placeholder="••••••••"
                            required
                        />
                    </div>

                    <button type="submit" className="login-button" disabled={loading}>
                        {loading ? 'Iniciando sesión...' : (
                            <>
                                <LogIn size={18} />
                                <span>Acceder al Sistema</span>
                            </>
                        )}
                    </button>
                </form>

                <footer>
                    <p>Instituto Chileno Norteamericano</p>
                </footer>
            </div>
        </div>
    );
};

export default Login;
