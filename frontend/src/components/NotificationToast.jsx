import React from 'react';
import { useSignalR } from '../context/SignalRContext';
import { motion, AnimatePresence } from 'framer-motion';
import { X, Bell } from 'lucide-react';

const NotificationToast = () => {
    const { notifications, removeNotification } = useSignalR();

    return (
        <div className="fixed bottom-5 right-5 z-50 flex flex-col gap-3">
            <AnimatePresence>
                {notifications.map((notif) => (
                    <motion.div
                        key={notif.id}
                        initial={{ opacity: 0, x: 50, scale: 0.9 }}
                        animate={{ opacity: 1, x: 0, scale: 1 }}
                        exit={{ opacity: 0, scale: 0.5, transition: { duration: 0.2 } }}
                        className="bg-white dark:bg-slate-800 border-l-4 border-blue-500 shadow-2xl rounded-lg p-4 w-80 flex items-start gap-3 relative overflow-hidden group"
                    >
                        <div className="bg-blue-100 dark:bg-blue-900/30 p-2 rounded-full">
                            <Bell className="w-5 h-5 text-blue-600 dark:text-blue-400" />
                        </div>
                        <div className="flex-1">
                            <p className="text-sm font-bold text-slate-800 dark:text-white">{notif.user}</p>
                            <p className="text-xs text-slate-600 dark:text-slate-300 mt-1">{notif.message}</p>
                            <p className="text-[10px] text-slate-400 mt-2">{notif.time}</p>
                        </div>
                        <button
                            onClick={() => removeNotification(notif.id)}
                            className="text-slate-400 hover:text-slate-600 dark:hover:text-white transition-colors"
                        >
                            <X className="w-4 h-4" />
                        </button>
                        <motion.div
                            initial={{ width: "100%" }}
                            animate={{ width: 0 }}
                            transition={{ duration: 5, ease: "linear" }}
                            onAnimationComplete={() => removeNotification(notif.id)}
                            className="absolute bottom-0 left-0 h-1 bg-blue-500/20"
                        />
                    </motion.div>
                ))}
            </AnimatePresence>
        </div>
    );
};

export default NotificationToast;
