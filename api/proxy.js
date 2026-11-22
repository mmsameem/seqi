// Vercel Serverless API Proxy for SEQI Platform
// Forwards /api and /uploads requests to the persistent backend server

export default async function handler(req, res) {
  const backendUrl = process.env.BACKEND_URL ? process.env.BACKEND_URL.replace(/\/+$/, '') : null;

  if (!backendUrl) {
    return res.status(503).json({
      success: false,
      error: 'Backend service not configured',
      message: 'The SEQI frontend is live on Vercel. To enable database authentication, scores, and faculty records, deploy the backend (e.g. on Render, Railway, or VPS) and add the BACKEND_URL environment variable in your Vercel Project Settings (e.g. BACKEND_URL=https://your-seqi-backend.onrender.com).'
    });
  }

  try {
    const targetUrl = `${backendUrl}${req.url}`;
    const forwardHeaders = { ...req.headers };
    delete forwardHeaders.host;
    delete forwardHeaders.connection;

    const fetchOptions = {
      method: req.method,
      headers: forwardHeaders
    };

    if (req.method !== 'GET' && req.method !== 'HEAD') {
      if (typeof req.body === 'object' && !Buffer.isBuffer(req.body)) {
        fetchOptions.body = JSON.stringify(req.body);
        forwardHeaders['content-type'] = 'application/json';
      } else if (req.body) {
        fetchOptions.body = req.body;
      }
    }

    const backendResponse = await fetch(targetUrl, fetchOptions);
    const data = await backendResponse.arrayBuffer();

    backendResponse.headers.forEach((val, key) => {
      // Skip hop-by-hop headers
      if (!['transfer-encoding', 'content-encoding'].includes(key.toLowerCase())) {
        res.setHeader(key, val);
      }
    });

    res.status(backendResponse.status).send(Buffer.from(data));
  } catch (error) {
    console.error('Vercel proxy error:', error);
    res.status(502).json({
      success: false,
      error: 'Unable to connect to backend server',
      details: error.message
    });
  }
}
