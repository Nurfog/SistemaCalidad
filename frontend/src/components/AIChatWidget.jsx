import React, { useState, useRef, useEffect } from 'react';
import { MessageSquare, Send, X, Bot, User, Loader2, Sparkles } from 'lucide-react';
import { motion, AnimatePresence } from 'framer-motion';
import api from '../api/client';
import '../styles/AIChatWidget.css'; // Crearemos este archivo de estilos

const AIChatWidget = ({ docId, docTitle }) => {
    const [isOpen, setIsOpen] = useState(false);
    const [messages, setMessages] = useState([
        { id: 1, role: 'ai', text: `Hola, soy tu asistente virtual experto en calidad. ¿Qué necesitas saber sobre "${docTitle}"?` }
    ]);
    const [input, setInput] = useState('');
    const [loading, setLoading] = useState(false);
    const messagesEndRef = useRef(null);

    const scrollToBottom = () => {
        messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
    };

    useEffect(() => {
        scrollToBottom();
    }, [messages, isOpen]);

    const handleSubmit = async (e) => {
        e.preventDefault();
        if (!input.trim() || loading) return;

        const userMsg = { id: Date.now(), role: 'user', text: input };
        setMessages(prev => [...prev, userMsg]);
        setInput('');
        setLoading(true);

        try {
            const response = await api.post(`/Documentos/${docId}/chat`, { pregunta: userMsg.text });
            const aiMsg = { id: Date.now() + 1, role: 'ai', text: response.data.respuesta };
            setMessages(prev => [...prev, aiMsg]);
        } catch (error) {
            console.error(error);
            setMessages(prev => [...prev, {
                id: Date.now() + 1,
                role: 'ai',
                text: "Lo siento, tuve un problema al analizar el documento. Por favor verifica los logs o intenta nuevamente."
            }]);
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="ai-chat-widget-container">
            <AnimatePresence>
                {isOpen && (
                    <motion.div
                        initial={{ opacity: 0, scale: 0.9, y: 20 }}
                        animate={{ opacity: 1, scale: 1, y: 0 }}
                        exit={{ opacity: 0, scale: 0.9, y: 20 }}
                        className="ai-chat-window"
                    >
                        <div className="ai-chat-header">
                            <div className="flex items-center gap-2">
                                <div className="bg-blue-100 p-1.5 rounded-lg">
                                    <Sparkles size={18} className="text-blue-600" />
                                </div>
                                <div>
                                    <h3 className="font-bold text-sm text-gray-800">Asistente SGC</h3>
                                    <p className="text-xs text-gray-500">Gemini 1.5 Flash</p>
                                </div>
                            </div>
                            <button onClick={() => setIsOpen(false)} className="text-gray-400 hover:text-gray-600">
                                <X size={18} />
                            </button>
                        </div>

                        <div className="ai-chat-messages">
                            {messages.map((msg) => (
                                <div key={msg.id} className={`message-bubble ${msg.role === 'user' ? 'user-msg' : 'ai-msg'}`}>
                                    {msg.role === 'ai' && <Bot size={16} className="mt-1 flex-shrink-0" />}
                                    <div className="message-content">
                                        <p>{msg.text}</p>
                                    </div>
                                    {msg.role === 'user' && <User size={16} className="mt-1 flex-shrink-0" />}
                                </div>
                            ))}
                            {loading && (
                                <div className="message-bubble ai-msg">
                                    <Bot size={16} />
                                    <div className="typing-indicator">
                                        <span></span><span></span><span></span>
                                    </div>
                                </div>
                            )}
                            <div ref={messagesEndRef} />
                        </div>

                        <form onSubmit={handleSubmit} className="ai-chat-input-area">
                            <input
                                type="text"
                                value={input}
                                onChange={(e) => setInput(e.target.value)}
                                placeholder="Pregunta algo sobre el archivo..."
                                disabled={loading}
                            />
                            <button type="submit" disabled={!input.trim() || loading} className={!input.trim() ? 'disabled' : ''}>
                                {loading ? <Loader2 size={18} className="animate-spin" /> : <Send size={18} />}
                            </button>
                        </form>
                    </motion.div>
                )}
            </AnimatePresence>

            {!isOpen && (
                <motion.button
                    whileHover={{ scale: 1.05 }}
                    whileTap={{ scale: 0.95 }}
                    onClick={() => setIsOpen(true)}
                    className="ai-fab-button"
                >
                    <Sparkles size={24} />
                    <span className="sr-only">Abrir Chat IA</span>
                </motion.button>
            )}
        </div>
    );
};

export default AIChatWidget;
