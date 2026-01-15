import React, { useState, useRef } from 'react';
import { useAuth } from '../../context/AuthContext';
import api from '../../api/client';
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

// RegEx CONSTANTS
// Soporta formatos:
// - "MDC-CC-1 Manual de Calidad Ver 27.pdf"
// - "PR-CC-01 Procedimiento Maestro.docx" (sin versión explícita)
// El código siempre termina en número: captura hasta el último dígito antes del título
// Mejorado para aceptar . , - o espacio antes de Ver/Rev
const FILE_REGEX = /^([A-Z]+-[A-Z]+-\d+)[-\s]+(.+?)(?:[-\s.,]+(?:Ver|Rev)\.?\s*(\d+))?\.([^.]+)$/i;

// Para carpetas: "1. Manual De Calidad PR-CC-03" -> "1. Manual De Calidad"
// El código al final sigue el patrón LETRAS-LETRAS-NÚMEROS
const FOLDER_REGEX = /^(.+?)(?:\s+([A-Z]+-[A-Z]+-\d+))?$/i;

// Función para convertir a Title Case (Primera Letra Mayúscula)
const toTitleCase = (str) => {
    return str
        .toLowerCase()
        .split(' ')
        .map(word => word.charAt(0).toUpperCase() + word.slice(1))
        .join(' ');
};

const BulkUploadDialog = ({ open, onClose, currentFolderId, onUploadComplete }) => {
    useAuth(); // Verificar autenticación
    const [items, setItems] = useState([]);
    const [uploading, setUploading] = useState(false);
    const [progress, setProgress] = useState(0);
    const [currentAction, setCurrentAction] = useState('');
    const fileInputRef = useRef(null);



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

    const [isDragging, setIsDragging] = useState(false);

    // Función recursiva para procesar entradas (FileSystemEntry) de Drag & Drop
    const scanEntries = async (entry, path = "") => {
        const processed = [];
        const currentPath = path ? `${path}/${entry.name}` : entry.name;

        if (entry.isFile) {
            const file = await new Promise((resolve) => entry.file(resolve));
            processed.push({ type: 'file', file, fullPath: currentPath });
        } else if (entry.isDirectory) {
            processed.push({ type: 'folder', name: entry.name, fullPath: currentPath });
            const reader = entry.createReader();
            const entries = await new Promise((resolve) => {
                let allEntries = [];
                const read = () => {
                    reader.readEntries((results) => {
                        if (results.length) {
                            allEntries = allEntries.concat(Array.from(results));
                            read();
                        } else {
                            resolve(allEntries);
                        }
                    });
                };
                read();
            });
            for (const childEntry of entries) {
                const childProcessed = await scanEntries(childEntry, currentPath);
                processed.push(...childProcessed);
            }
        }
        return processed;
    };

    const processFiles = async (inputFiles, fromDrag = false) => {
        const processedItems = [];
        const foldersMap = new Set();

        let rawItems = [];

        if (fromDrag) {
            // Caso Drag & Drop: Usamos FileSystemEntry para recursión real
            for (const item of inputFiles) {
                const entry = item.webkitGetAsEntry();
                if (entry) {
                    const results = await scanEntries(entry);
                    rawItems.push(...results);
                }
            }
        } else {
            // Caso Botón: Usamos webkitRelativePath
            rawItems = Array.from(inputFiles).map(f => ({
                type: 'file',
                file: f,
                fullPath: f.webkitRelativePath
            }));
        }

        const fileGroups = {}; // Agrupación GLOBAL para de-duplicación: { "Key": [archivos] }

        rawItems.forEach(item => {
            const pathParts = item.fullPath.split('/');
            const fileName = pathParts.pop();

            // 0. Si el item es una carpeta, asegurar que se procese ella misma también
            if (item.type === 'folder' && !foldersMap.has(item.fullPath)) {
                const parentPath = pathParts.join('/');
                let newFolderName = item.name;
                const match = item.name.match(FOLDER_REGEX);
                if (match) {
                    newFolderName = toTitleCase(match[1].trim());
                } else {
                    newFolderName = toTitleCase(item.name);
                }

                processedItems.push({
                    type: 'folder',
                    originalName: item.name,
                    newName: newFolderName,
                    path: item.fullPath,
                    parentPath: parentPath,
                    depth: pathParts.length,
                    status: 'pending'
                });
                foldersMap.add(item.fullPath);
            }

            // 1. Asegurar que TODAS las carpetas intermedias existan en el mapa
            let currentPath = "";
            pathParts.forEach((folderRawName, index) => {
                const parentPath = currentPath;
                currentPath = currentPath ? `${currentPath}/${folderRawName}` : folderRawName;

                if (!foldersMap.has(currentPath)) {
                    let newFolderName = folderRawName;
                    const match = folderRawName.match(FOLDER_REGEX);
                    if (match) {
                        newFolderName = toTitleCase(match[1].trim());
                    } else {
                        newFolderName = toTitleCase(folderRawName);
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

            // 2. Si es un archivo, agruparlo para elegir la mejor versión (Detección Global de Duplicados)
            if (item.type === 'file') {
                const extension = item.file.name.split('.').pop().toLowerCase();
                const baseName = item.file.name.replace(`.${extension}`, '');

                // Extraer info preliminar para el join
                let docCode = '';
                const matchFile = item.file.name.match(FILE_REGEX);
                if (matchFile) {
                    docCode = matchFile[1].toUpperCase();
                } else {
                    const simpleCodeMatch = baseName.match(/^([A-Z]+-[A-Z]+-\d+)/i);
                    if (simpleCodeMatch) docCode = simpleCodeMatch[1].toUpperCase();
                }

                // Si tiene código, agrupamos por código. Si no, por título (baseName)
                const groupKey = docCode || baseName.toLowerCase().trim();

                if (!fileGroups[groupKey]) fileGroups[groupKey] = [];
                fileGroups[groupKey].push({ ...item, extension, baseName, parentPath: currentPath, docCode });
            }
        });

        // 3. Procesar los grupos de archivos (De-duplicación con Prioridad + Acumulación de Carpetas)
        const priority = { 'docx': 4, 'xlsx': 3, 'xls': 2, 'pdf': 1 };

        Object.values(fileGroups).forEach(group => {
            // Ordenar por prioridad de extensión y luego por tamaño
            group.sort((a, b) => {
                const pA = priority[a.extension] || 0;
                const pB = priority[b.extension] || 0;
                if (pA !== pB) return pB - pA;
                return b.file.size - a.file.size;
            });

            // Acumular todas las carpetas donde debe estar este documento
            const allParentPaths = Array.from(new Set(group.map(g => g.parentPath)));

            const best = group[0];
            const fileName = best.file.name;
            let newTitle = best.baseName;
            let docCode = best.docCode;
            let docVersion = 1;
            const matchFile = fileName.match(FILE_REGEX);

            if (matchFile) {
                newTitle = toTitleCase(matchFile[2].trim());
                docVersion = matchFile[3] ? parseInt(matchFile[3], 10) : 1;
            } else {
                const simpleCodeMatch = best.baseName.match(/^([A-Z]+-[A-Z]+-\d+)(.*)/i);
                if (simpleCodeMatch) {
                    newTitle = toTitleCase(simpleCodeMatch[2].trim() || simpleCodeMatch[1]);
                } else {
                    newTitle = toTitleCase(best.baseName);
                }
            }

            if (!docCode) {
                docCode = newTitle.toUpperCase().substring(0, 45).replace(/\s+/g, '-');
            }

            processedItems.push({
                type: 'file',
                originalName: fileName,
                newName: newTitle,
                extension: best.extension.toUpperCase(),
                code: docCode,
                version: docVersion,
                path: best.fullPath,
                parentPaths: allParentPaths, // Lista de todas las ubicaciones
                parentPath: best.parentPath, // Una de ellas para compatibilidad en tabla
                fileObj: best.file,
                status: 'pending'
            });
        });

        // Ordenar: Carpetas primero por profundidad, luego archivos
        processedItems.sort((a, b) => {
            if (a.type === 'folder' && b.type === 'file') return -1;
            if (a.type === 'file' && b.type === 'folder') return 1;
            if (a.type === 'folder' && b.type === 'folder') return a.depth - b.depth;
            return 0;
        });

        setItems(processedItems);
    };

    const handleDragOver = (e) => {
        e.preventDefault();
        setIsDragging(true);
    };

    const handleDragLeave = () => setIsDragging(false);

    const handleDrop = async (e) => {
        e.preventDefault();
        setIsDragging(false);
        if (e.dataTransfer.items) {
            await processFiles(e.dataTransfer.items, true);
        }
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
                const parentId = item.parentPath ? createdFoldersIds[item.parentPath] : currentFolderId;

                if (item.type === 'folder') {
                    if (!parentId && item.parentPath !== "") throw new Error("Padre no encontrado");

                    const response = await api.post('/CarpetasDocumentos', {
                        nombre: item.newName,
                        parentId: parentId,
                        color: '#fbbf24'
                    });

                    createdFoldersIds[item.path] = response.data.id;

                } else {
                    const formData = new FormData();
                    formData.append('titulo', item.newName);
                    formData.append('codigo', item.code);
                    formData.append('tipo', 0);
                    formData.append('area', 0);

                    // Enviar todos los IDs de carpeta
                    if (item.parentPaths && item.parentPaths.length > 0) {
                        item.parentPaths.forEach(path => {
                            const id = createdFoldersIds[path];
                            if (id) formData.append('carpetaIds', id);
                        });
                    } else if (parentId) {
                        formData.append('carpetaIds', parentId);
                    }

                    formData.append('numeroRevision', item.version);

                    // [FIX] Forzar nombre limpio: "CODIGO - Titulo.ext" o "Titulo.ext" si no hay código
                    // Limpiamos caracteres ilegales para mayor seguridad aunque el navegador suele manejarlos
                    const cleanTitle = item.newName.replace(/[\\/:*?"<>|]/g, '');
                    const finalFileName = item.code
                        ? `${item.code} - ${cleanTitle}.${item.extension.toLowerCase()}`
                        : `${cleanTitle}.${item.extension.toLowerCase()}`;

                    formData.append('archivo', item.fileObj, finalFileName);

                    await api.post('/Documentos', formData, {
                        headers: { 'Content-Type': 'multipart/form-data' }
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
                const serverMsg = error.response?.data ? (typeof error.response.data === 'string' ? error.response.data : error.response.data.mensaje) : error.message;

                // Si la carpeta falla, marcamos que no se pudo crear para los hijos
                if (item.type === 'folder') {
                    createdFoldersIds[item.path] = null;
                }

                setItems(prev => {
                    const newArr = [...prev];
                    newArr[i] = { ...newArr[i], status: 'error', errorMsg: serverMsg };
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
                        <div
                            style={{
                                ...styles.dropZone,
                                borderColor: isDragging ? '#4f46e5' : '#ccc',
                                backgroundColor: isDragging ? '#f5f7ff' : '#fafafa',
                                borderStyle: isDragging ? 'solid' : 'dashed'
                            }}
                            onClick={() => fileInputRef.current.click()}
                            onDragOver={handleDragOver}
                            onDragLeave={handleDragLeave}
                            onDrop={handleDrop}
                        >
                            <input
                                type="file"
                                ref={fileInputRef}
                                webkitdirectory=""
                                directory=""
                                multiple
                                style={{ display: 'none' }}
                                onChange={(e) => processFiles(e.target.files)}
                            />
                            <CloudUpload size={48} color={isDragging ? "#4f46e5" : "#ccc"} />
                            <p style={{ marginTop: '16px', fontWeight: '500' }}>
                                {isDragging ? 'Suelta para cargar' : 'Clic o Arrastra Carpeta/Archivos'}
                            </p>
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
                                        <th style={styles.th}>Tipo</th>
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
                                                {item.type === 'file' && (
                                                    <span style={{
                                                        fontSize: '0.7rem',
                                                        padding: '2px 4px',
                                                        borderRadius: '4px',
                                                        backgroundColor: '#f1f5f9',
                                                        color: '#475569',
                                                        fontWeight: '600'
                                                    }}>
                                                        {item.extension}
                                                    </span>
                                                )}
                                                {item.type === 'folder' && '-'}
                                            </td>
                                            <td style={styles.td}>
                                                {item.status === 'pending' && <span style={{ color: '#999', fontSize: '0.8rem' }}>Pendiente</span>}
                                                {item.status === 'success' && <CheckCircle size={16} color="#10b981" />}
                                                {item.status === 'error' && (
                                                    <div style={{ display: 'flex', flexDirection: 'column', color: '#ef4444' }}>
                                                        <div style={{ display: 'flex', alignItems: 'center', gap: '4px' }}>
                                                            <AlertCircle size={16} />
                                                            <span style={{ fontSize: '0.75rem', fontWeight: 'bold' }}>Error</span>
                                                        </div>
                                                        <div style={{ fontSize: '0.7rem', marginTop: '2px' }}>{item.errorMsg}</div>
                                                    </div>
                                                )}
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
