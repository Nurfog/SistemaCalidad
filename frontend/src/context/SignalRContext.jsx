import React, { createContext, useContext, useEffect, useState } from 'react';
import * as signalR from '@microsoft/signalr';

const SignalRContext = createContext();

export const SignalRProvider = ({ children }) => {
    const [connection, setConnection] = useState(null);
    const [notifications, setNotifications] = useState([]);

    useEffect(() => {
        const newConnection = new signalR.HubConnectionBuilder()
            .withUrl(`${import.meta.env.VITE_API_URL || 'http://localhost:5156'}/hub/notificaciones`, {
                skipNegotiation: true,
                transport: signalR.HttpTransportType.WebSockets
            })
            .withAutomaticReconnect()
            .build();

        setConnection(newConnection);
    }, []);

    useEffect(() => {
        if (connection) {
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
                        // Aquí se podría disparar un toast
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
