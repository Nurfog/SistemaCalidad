import React, { createContext, useContext, useEffect, useState } from 'react';
import * as signalR from '@microsoft/signalr';
import { useAuth } from './AuthContext';

const SignalRContext = createContext();

export const SignalRProvider = ({ children }) => {
    const [connection, setConnection] = useState(null);
    const [notifications, setNotifications] = useState([]);

    const { user } = useAuth(); // Asumiendo que AuthContext exporta useAuth

    useEffect(() => {
        // Solo conectar si hay usuario autenticado
        if (!user) return;

        const token = localStorage.getItem('token');
        if (!token) return;

        const apiUrl = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5156';

        const newConnection = new signalR.HubConnectionBuilder()
            .withUrl(`${apiUrl}/hub/notificaciones`, { // Evitar doble /api si la base ya lo tiene
                accessTokenFactory: () => token
            })
            .withAutomaticReconnect()
            .build();

        setConnection(newConnection);

        return () => {
            if (newConnection) {
                newConnection.stop();
            }
        };
    }, [user]);

    useEffect(() => {
        if (connection && connection.state === signalR.HubConnectionState.Disconnected) {
            connection.start()
                .then(() => {
                    console.log('[SignalR] Conectado');
                    connection.on('ReceiveNotification', (user, message) => {
                        const newNotification = {
                            id: Date.now(),
                            user,
                            message,
                            time: new Date().toLocaleTimeString()
                        };
                        setNotifications(prev => [newNotification, ...prev].slice(0, 5));
                    });
                })
                .catch(e => console.log('[SignalR] Error de conexión: ', e));
        }
    }, [connection]);

    const removeNotification = (id) => {
        setNotifications(prev => prev.filter(n => n.id !== id));
    };

    return (
        <SignalRContext.Provider value={{ notifications, removeNotification }}>
            {children}
        </SignalRContext.Provider>
    );
};

export const useSignalR = () => useContext(SignalRContext);
