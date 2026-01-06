import React, { useState, useEffect } from 'react';
import { Document, Page, pdfjs } from 'react-pdf';
import { X, ChevronLeft, ChevronRight, ShieldAlert, Ban } from 'lucide-react';
import '../styles/SecureDocViewer.css';
import 'react-pdf/dist/Page/TextLayer.css';
import 'react-pdf/dist/Page/AnnotationLayer.css';
import AIChatWidget from './AIChatWidget';

// Configurar Worker de PDF.js para Vite
pdfjs.GlobalWorkerOptions.workerSrc = `//unpkg.com/pdfjs-dist@${pdfjs.version}/build/pdf.worker.min.mjs`;

const SecureDocViewer = ({ fileUrl, fileName, onClose, user, docId }) => {
    const [numPages, setNumPages] = useState(null);
    const [pageNumber, setPageNumber] = useState(1);
    const [scale, setScale] = useState(1.2);

    // Deshabilitar menú contextual (Click derecho)
    useEffect(() => {
        const handleContextMenu = (e) => {
            e.preventDefault();
            return false;
        };

        // Deshabilitar atajos de teclado para copiar/imprimir
        const handleKeyDown = (e) => {
            if ((e.ctrlKey || e.metaKey) && (e.key === 'c' || e.key === 'p' || e.key === 's')) {
                e.preventDefault();
                alert('La copia e impresión de documentos está restringida por políticas de seguridad.');
            }
        };

        document.addEventListener('contextmenu', handleContextMenu);
        document.addEventListener('keydown', handleKeyDown);

        return () => {
            document.removeEventListener('contextmenu', handleContextMenu);
            document.removeEventListener('keydown', handleKeyDown);
        };
    }, []);

    function onDocumentLoadSuccess({ numPages }) {
        setNumPages(numPages);
    }

    const changePage = (offset) => {
        setPageNumber(prevPageNumber => prevPageNumber + offset);
    };

    const previousPage = () => changePage(-1);
    const nextPage = () => changePage(1);

    // Generar marcas de agua repetitivas
    const watermarkContent = [];
    for (let i = 0; i < 20; i++) {
        watermarkContent.push(
            <div key={i} className="watermark-text">
                SOLO LECTURA - {user?.nombre || 'USUARIO'} - {new Date().toLocaleDateString()}
            </div>
        );
    }

    return (
        <div className="secure-viewer-overlay">
            {/* Header de Seguridad */}
            <div className="secure-viewer-header">
                <div className="doc-title">
                    <ShieldAlert size={20} className="text-red-500" />
                    <span>{fileName}</span>
                    <span className="security-badge">
                        <Ban size={12} style={{ display: 'inline', marginRight: 4 }} />
                        NO COPIAR
                    </span>
                </div>
                <button className="close-viewer-btn" onClick={onClose}>
                    <X size={24} />
                </button>
            </div>

            {/* Contenedor del PDF */}
            <div className="pdf-container" onContextMenu={(e) => e.preventDefault()}>

                {/* Capa invisible para interceptar clicks */}
                <div className="security-layer"></div>

                {/* Marca de agua Overlay */}
                <div className="watermark-overlay">
                    {watermarkContent}
                </div>

                <Document
                    file={fileUrl}
                    onLoadSuccess={onDocumentLoadSuccess}
                    loading={
                        <div className="text-white flex flex-col items-center">
                            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-white mb-2"></div>
                            Cargando documento seguro...
                        </div>
                    }
                    error={
                        <div className="text-red-400 p-4 bg-gray-800 rounded-lg">
                            Error al cargar el documento protegido. Contacte al administrador.
                        </div>
                    }
                >
                    <div className="pdf-page-wrapper">
                        <Page
                            pageNumber={pageNumber}
                            scale={scale}
                            renderTextLayer={false} /* Deshabilitar capa de texto seleccionable */
                            renderAnnotationLayer={false}
                            className="shadow-2xl"
                        />
                    </div>
                </Document>

                {/* Controles Flotantes */}
                <div className="viewer-controls">
                    <button disabled={pageNumber <= 1} onClick={previousPage}>
                        <ChevronLeft size={24} />
                    </button>
                    <span className="page-indicator">
                        Página {pageNumber} de {numPages || '--'}
                    </span>
                    <button disabled={pageNumber >= numPages} onClick={nextPage}>
                        <ChevronRight size={24} />
                    </button>

                    <div className="w-px h-6 bg-gray-600 mx-2"></div>

                    <button onClick={() => setScale(s => Math.max(0.5, s - 0.2))}>-</button>
                    <span className="text-sm text-gray-400">Zoom</span>
                    <button onClick={() => setScale(s => Math.min(2.0, s + 0.2))}>+</button>
                </div>
            </div>

            {/* Widget de Chat con IA */}
            {docId && (
                <AIChatWidget docId={docId} docTitle={fileName} />
            )}
        </div>
    );
};

export default SecureDocViewer;
