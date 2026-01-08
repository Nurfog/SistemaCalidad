import { useState, useEffect } from 'react';
import { ChevronRight, ChevronDown, Folder, Home, Plus, Trash2 } from 'lucide-react';
import api from '../api/client';

const FolderTreeItem = ({ carpeta, level, onSelect, selectedId, expandedIds, toggleExpand, onAddClick, onDeleteClick }) => {
    const isExpanded = expandedIds.includes(carpeta.id);
    const isSelected = selectedId === carpeta.id;

    const handleToggle = (e) => {
        e.stopPropagation();
        toggleExpand(carpeta.id);
    };

    return (
        <div className="tree-item-container">
            <div
                className={`tree-item ${isSelected ? 'active' : ''}`}
                style={{ paddingLeft: `${level * 16 + 8}px` }}
                onClick={() => onSelect(carpeta)}
            >
                <button className="expand-btn" onClick={handleToggle} disabled={!carpeta.hasChildren && !carpeta.subCarpetas?.length}>
                    {isExpanded ? <ChevronDown size={14} /> : <ChevronRight size={14} />}
                </button>
                <Folder size={16} className="folder-icon" fill={carpeta.color || '#3b82f6'} fillOpacity={0.2} style={{ color: carpeta.color || '#3b82f6' }} />
                <span className="folder-name">{carpeta.nombre}</span>
                <button
                    className="add-subfolder-btn"
                    onClick={(e) => { e.stopPropagation(); onAddClick(carpeta); }}
                    title="Nueva subcarpeta aquí"
                >
                    <Plus size={14} />
                </button>
                <button
                    className="delete-folder-btn"
                    onClick={(e) => { e.stopPropagation(); onDeleteClick(carpeta); }}
                    title="Eliminar carpeta"
                >
                    <Trash2 size={14} />
                </button>
            </div>

            {isExpanded && Array.isArray(carpeta.subCarpetas) && (
                <div className="tree-children">
                    {carpeta.subCarpetas.map(sub => (
                        <FolderTreeItem
                            key={sub.id}
                            carpeta={sub}
                            level={level + 1}
                            onSelect={onSelect}
                            selectedId={selectedId}
                            expandedIds={expandedIds}
                            toggleExpand={toggleExpand}
                            onAddClick={onAddClick}
                            onDeleteClick={onDeleteClick}
                        />
                    ))}
                </div>
            )}
        </div>
    );
};

const FolderTree = ({ onSelect, selectedId, refreshKey, onAddClick, onDeleteClick }) => {
    const [treeData, setTreeData] = useState([]);
    const [expandedIds, setExpandedIds] = useState([]);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        fetchFullTree();
    }, [refreshKey]);

    const fetchFullTree = async () => {
        setLoading(true);
        try {
            // El backend ya tiene CarpetasDocumentos. 
            // Para un árbol real, necesitamos que el backend nos de la estructura.
            // Si no la da, la construimos aquí a partir de la lista plana o peticiones.
            const response = await api.get('/CarpetasDocumentos/Arbol');
            setTreeData(Array.isArray(response.data) ? response.data : []);
        } catch (error) {
            console.error('Error al cargar árbol de carpetas:', error);
            const fallback = await api.get('/CarpetasDocumentos', { params: { parentId: null } });
            setTreeData(Array.isArray(fallback.data) ? fallback.data : []);
        } finally {
            setLoading(false);
        }
    };

    const toggleExpand = async (id) => {
        if (expandedIds.includes(id)) {
            setExpandedIds(expandedIds.filter(eid => eid !== id));
        } else {
            setExpandedIds([...expandedIds, id]);

            // Cargar hijos si no están cargados (Lazy loading)
            const item = findItemInTree(treeData, id);
            if (item && (!item.subCarpetas || item.subCarpetas.length === 0)) {
                try {
                    const res = await api.get('/CarpetasDocumentos', { params: { parentId: id } });
                    if (res.data.length > 0) {
                        updateTreeItem(id, res.data);
                    }
                } catch (e) { console.error(e); }
            }
        }
    };

    const findItemInTree = (items, id) => {
        for (const item of items) {
            if (item.id === id) return item;
            if (item.subCarpetas) {
                const found = findItemInTree(item.subCarpetas, id);
                if (found) return found;
            }
        }
        return null;
    };

    const updateTreeItem = (id, children) => {
        const update = (items) => items.map(item => {
            if (item.id === id) return { ...item, subCarpetas: children };
            if (item.subCarpetas) return { ...item, subCarpetas: update(item.subCarpetas) };
            return item;
        });
        setTreeData(update(treeData));
    };

    return (
        <aside className="folder-tree-sidebar">
            <div className="tree-header">
                <h3>Explorador SGC</h3>
            </div>
            <div className="tree-content">
                <div
                    className={`tree-item root-item ${selectedId === null ? 'active' : ''}`}
                    onClick={() => onSelect(null)}
                >
                    <Home size={16} />
                    <span>Raíz del Sistema</span>
                </div>

                {loading && treeData.length === 0 ? (
                    <div className="tree-loading">Cargando...</div>
                ) : (
                    (Array.isArray(treeData) ? treeData : []).map(carpeta => (
                        <FolderTreeItem
                            key={carpeta.id}
                            carpeta={carpeta}
                            level={0}
                            onSelect={onSelect}
                            selectedId={selectedId}
                            expandedIds={expandedIds}
                            toggleExpand={toggleExpand}
                            onAddClick={onAddClick}
                            onDeleteClick={onDeleteClick}
                        />
                    ))
                )}
            </div>
        </aside>
    );
};

export default FolderTree;
