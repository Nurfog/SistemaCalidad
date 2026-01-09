import React, { useState, useRef } from 'react';
import axios from 'axios';
import { useAuth } from '../../context/AuthContext';
import {
    CloudUpload,
    Trash2,
    CheckCircle,
    AlertCircle,
    X,
    Folder,
    File,
    Loader2
} from 'lucide-react';

// RegEx CONSTANTS (Mismos que antes)
const FILE_REGEX = /^([A-Z0-9-]+)\s+(.+)\s+Ver\s+(\d+)\.(.+)$/i;
const FOLDER_REGEX = /^(.*)\s+([A-Z0-9-]+)$/i;

const BulkUploadDialog = ({ open, onClose, currentFolderId, onUploadComplete }) => {
    const { token } = useAuth();
    const [items, setItems] = useState([]);
    const [uploading, setUploading] = useState(false);
    const [progress, setProgress] = useState(0);
    const [currentAction, setCurrentAction] = useState('');
    const fileInputRef = useRef(null);

    const API_URL = import.meta.env.VITE_API_BASE_URL;

    // Estilos inline para reemplazar MUI (Solución rápida y compatible)
    const styles = {
        overlay: {
            position: 'fixed', top: 0, left: 0, right: 0, bottom: 0,
            backgroundColor: 'rgba(0,0,0,0.5)', zIndex: 1300,
            display: 'flex', alignItems: 'center', justifyContent: 'center'
        },
        dialog: {
            backgroundColor: '#fff', borderRadius: '8px',
            width: '90%', maxWidth: '1000px', maxHeight: '90vh',
            display: 'flex', flexDirection: 'column',
            boxShadow: '0 4px 20px rgba(0,0,0,0.15)',
            position: 'relative',
            color: '#333'
        },
        header: {
            padding: '16px 24px', borderBottom: '1px solid #eee',
            display: 'flex', justifyContent: 'space-between', alignItems: 'center'
        },
        content: {
            padding: '24px', overflowY: 'auto', flex: 1
        },
        actions: {
            padding: '16px 24px', borderTop: '1px solid #eee',
            display: 'flex', justifyContent: 'flex-end', gap: '12px'
        },
        dropZone: {
            border: '2px dashed #ccc', borderRadius: '8px', padding: '40px',
            textAlign: 'center', cursor: 'pointer', backgroundColor: '#fafafa'
        },
        table: {
            width: '100%', borderCollapse: 'collapse', marginTop: '16px', fontSize: '0.9rem'
        },
        th: { padding: '8px', borderBottom: '2px solid #eee', textAlign: 'left', color: '#666' },
        td: { padding: '8px', borderBottom: '1px solid #eee' },
        rowError: { backgroundColor: '#fff5f5' },
        rowSuccess: { backgroundColor: '#f0fff4' },
        progressBar: {
            height: '4px', backgroundColor: '#eee', borderRadius: '2px', overflow: 'hidden', marginTop: '8px'
        },
        progressFill: {
            height: '100%', backgroundColor: '#4f46e5', transition: 'width 0.3s ease'
        }
    };

    if (!open) return null;

    const handleFolderSelect = (event) => {
        const files = Array.from(event.target.files);
        if (files.length === 0) return;

        const processedItems = [];
        const foldersMap = new Set();

        files.forEach(file => {
            const pathParts = file.webkitRelativePath.split('/');
            const fileName = pathParts.pop();

            // 1. Procesar CARPETAS
            let currentPath = "";
            pathParts.forEach((folderRawName, index) => {
                const parentPath = currentPath;
                currentPath = currentPath ? `${currentPath}/${folderRawName}` : folderRawName;

                if (!foldersMap.has(currentPath)) {
                    let newFolderName = folderRawName;
                    const match = folderRawName.match(FOLDER_REGEX);
                    if (match) {
                        newFolderName = match[1].trim();
                    }

                    processedItems.push({
                        type: 'folder',
                        originalName: folderRawName,
                        newName: newFolderName,
                        path: currentPath,
                        parentPath: parentPath,
                        depth: index,
                        status: 'pending'
                    });
                    foldersMap.add(currentPath);
                }
            });

            // 2. Procesar ARCHIVO
            let newTitle = fileName;
            let docCode = '';
            let docVersion = 1;
            let extension = fileName.split('.').pop();
            const matchFile = fileName.match(FILE_REGEX);

            if (matchFile) {
                docCode = matchFile[1];
                newTitle = matchFile[2].trim();
                docVersion = parseInt(matchFile[3], 10);
            } else {
                const justName = fileName.replace(`.${extension}`, '');
                newTitle = justName;
            }

            processedItems.push({
                type: 'file',
                originalName: fileName,
                newName: newTitle,
                code: docCode,
                version: docVersion,
                path: file.webkitRelativePath,
                parentPath: currentPath,
                fileObj: file,
                status: 'pending'
            });
        });

        processedItems.sort((a, b) => {
            if (a.type === 'folder' && b.type === 'file') return -1;
            if (a.type === 'file' && b.type === 'folder') return 1;
            if (a.type === 'folder' && b.type === 'folder') return a.depth - b.depth;
            return 0;
        });

        setItems(processedItems);
    };

    const executeUpload = async () => {
        setUploading(true);
        setProgress(0);
        const createdFoldersIds = { "": currentFolderId };
        let successCount = 0;

        for (let i = 0; i < items.length; i++) {
            const item = items[i];
            setCurrentAction(`Procesando: ${item.newName}`);

            try {
                if (item.type === 'folder') {
                    const parentId = item.parentPath ? createdFoldersIds[item.parentPath] : currentFolderId;
                    if (!parentId && item.parentPath !== "") throw new Error("Padre no encontrado");

                    const response = await axios.post(`${API_URL}/CarpetasDocumentos`, {
                        nombre: item.newName,
                        parentId: parentId,
                        color: '#fbbf24'
                    }, { headers: { Authorization: `Bearer ${token}` } });

                    createdFoldersIds[item.path] = response.data.id;

                } else {
                    const parentId = item.parentPath ? createdFoldersIds[item.parentPath] : currentFolderId;
                    const formData = new FormData();
                    formData.append('titulo', item.newName);
                    formData.append('codigo', item.code || 'S/C');
                    formData.append('tipo', 0);
                    formData.append('area', 0);
                    formData.append('carpetaId', parentId || '');
                    formData.append('numeroRevision', item.version);
                    formData.append('archivo', item.fileObj);

                    await axios.post(`${API_URL}/Documentos`, formData, {
                        headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'multipart/form-data' }
                    });
                }

                setItems(prev => {
                    const newArr = [...prev];
                    newArr[i] = { ...newArr[i], status: 'success' };
                    return newArr;
                });
                successCount++;

            } catch (error) {
                console.error(error);
                setItems(prev => {
                    const newArr = [...prev];
                    newArr[i] = { ...newArr[i], status: 'error' };
                    return newArr;
                });
            }
            setProgress(Math.round(((i + 1) / items.length) * 100));
        }
        setUploading(false);
        setCurrentAction(`Finalizado: ${successCount} items.`);
        setTimeout(() => { if (onUploadComplete) onUploadComplete(); onClose(); }, 1500);
    };

    const reset = () => {
        setItems([]);
        setUploading(false);
        setProgress(0);
    };

    return (
        <div style={styles.overlay}>
            <div style={styles.dialog}>

                {/* HEADER */}
                <div style={styles.header}>
                    <div>
                        <h2 style={{ margin: 0, fontSize: '1.25rem' }}>Carga Masiva Inteligente</h2>
                        <span style={{ fontSize: '0.875rem', color: '#666' }}>Sube estructuras completas y limpia nombres automáticamente.</span>
                    </div>
                    {!uploading && <button onClick={onClose} style={{ background: 'none', border: 'none', cursor: 'pointer' }}><X size={24} /></button>}
                </div>

                {/* CONTENT */}
                <div style={styles.content}>
                    {!items.length ? (
                        <div style={styles.dropZone} onClick={() => fileInputRef.current.click()}>
                            <input type="file" ref={fileInputRef} webkitdirectory="" directory="" multiple style={{ display: 'none' }} onChange={handleFolderSelect} />
                            <CloudUpload size={48} color="#ccc" />
                            <p style={{ marginTop: '16px', fontWeight: '500' }}>Clic para seleccionar Carpeta</p>
                            <p style={{ fontSize: '0.8rem', color: '#888' }}>Mantiene subcarpetas y jerarquía.</p>
                        </div>
                    ) : (
                        <div>
                            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px' }}>
                                <strong>{items.length} items detectados</strong>
                                {!uploading && (
                                    <button onClick={reset} style={{ color: '#ef4444', background: 'none', border: 'none', cursor: 'pointer', display: 'flex', alignItems: 'center', gap: '4px' }}>
                                        <Trash2 size={16} /> Limpiar
                                    </button>
                                )}
                            </div>

                            <table style={styles.table}>
                                <thead>
                                    <tr>
                                        <th style={styles.th}></th>
                                        <th style={styles.th}>Ruta Original</th>
                                        <th style={styles.th}>Nombre SGC</th>
                                        <th style={styles.th}>Cód</th>
                                        <th style={styles.th}>Ver</th>
                                        <th style={styles.th}>Estado</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {items.map((item, idx) => (
                                        <tr key={idx} style={item.status === 'error' ? styles.rowError : item.status === 'success' ? styles.rowSuccess : {}}>
                                            <td style={styles.td}>{item.type === 'folder' ? <Folder size={16} color="#fbbf24" /> : <File size={16} color="#64748b" />}</td>
                                            <td style={styles.td} title={item.path}>
                                                <div style={{ maxWidth: '200px', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis', color: '#666', fontSize: '0.8rem' }}>
                                                    {item.path}
                                                </div>
                                            </td>
                                            <td style={styles.td}><strong>{item.newName}</strong></td>
                                            <td style={styles.td}>{item.code || '-'}</td>
                                            <td style={styles.td}>{item.version || '-'}</td>
                                            <td style={styles.td}>
                                                {item.status === 'pending' && <span style={{ color: '#999', fontSize: '0.8rem' }}>Pendiente</span>}
                                                {item.status === 'success' && <CheckCircle size={16} color="#10b981" />}
                                                {item.status === 'error' && <AlertCircle size={16} color="#ef4444" />}
                                            </td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>

                            {uploading && (
                                <div style={{ marginTop: '24px' }}>
                                    <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.8rem', marginBottom: '8px' }}>
                                        <span>{currentAction}</span>
                                        <span>{progress}%</span>
                                    </div>
                                    <div style={styles.progressBar}>
                                        <div style={{ ...styles.progressFill, width: `${progress}%` }}></div>
                                    </div>
                                </div>
                            )}
                        </div>
                    )}
                </div>

                {/* ACTIONS */}
                <div style={styles.actions}>
                    <button onClick={onClose} disabled={uploading} style={{ padding: '8px 16px', borderRadius: '6px', border: '1px solid #ccc', background: 'white', cursor: 'pointer' }}>Cancelar</button>
                    {items.length > 0 && !uploading && (
                        <button onClick={executeUpload} style={{ padding: '8px 16px', borderRadius: '6px', border: 'none', background: '#4f46e5', color: 'white', cursor: 'pointer', display: 'flex', alignItems: 'center', gap: '8px' }}>
                            <CloudUpload size={16} /> Iniciar Carga
                        </button>
                    )}
                </div>
            </div>
        </div>
    );
};

export default BulkUploadDialog;
