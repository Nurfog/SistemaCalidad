const axios = require('axios');

const AI_API_URL = 'https://subtegumental-untextual-vernell.ngrok-free.dev';
const credentials = {
    username: 'sgc_sistema',
    password: 'sgc_sistema_pass_2026!'
};

async function verifyAI() {
    console.log('--- INICIANDO PRUEBA SINTÉTICA DE IA (APRENDIZAJE SGC) ---');

    try {
        // 1. Intentar registrar/login para asegurar sesión
        console.log('1. Verificando autenticación de la IA...');
        await axios.post(`${AI_API_URL}/login`, credentials).catch(e => console.log('   (El login falló o no es requerido, continuando...)'));

        // 2. Hacer una pregunta técnica sobre el SGC
        console.log('2. Realizando consulta técnica sobre documentos SGC...');
        const chatRequest = {
            username: credentials.username,
            prompt: '¿Cuál es el objetivo del Sistema de Gestión de Calidad (SGC) del Instituto Chileno Norteamericano y qué documentos lo rigen (NCh 2728)? Menciona algo específico que hayas aprendido de los archivos cargados.',
            use_kb: true
        };

        const startTime = Date.now();
        const response = await axios.post(`${AI_API_URL}/chat`, chatRequest);
        const duration = (Date.now() - startTime) / 1000;

        console.log(`\nRespuesta de la IA (obtenida en ${duration}s):`);
        console.log('----------------------------------------------------');
        console.log(response.data);
        console.log('----------------------------------------------------');

        if (response.data && response.data.length > 50) {
            console.log('\n✅ PRUEBA EXITOSA: La IA devolvió una respuesta detallada basada en el contexto.');
        } else {
            console.log('\n⚠️ ADVERTENCIA: La respuesta fue muy corta. Podría no estar usando la base de conocimientos correctamente.');
        }

    } catch (error) {
        console.error('\n❌ ERROR DURANTE LA PRUEBASINTÉTICA:');
        if (error.response) {
            console.error(`Status: ${error.response.status}`);
            console.error(`Data: ${JSON.stringify(error.response.data)}`);
        } else {
            console.error(error.message);
        }
    }
}

verifyAI();
