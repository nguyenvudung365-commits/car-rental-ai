import React, { useState, useEffect } from 'react'

function App() {
  const [health, setHealth] = useState(null)
  const [error, setError] = useState(null)

  useEffect(() => {
    fetch('/api/health')
      .then(res => res.json())
      .then(data => setHealth(data))
      .catch(err => setError(err.message))
  }, [])

  return (
    <div style={{ padding: '2rem', fontFamily: 'system-ui' }}>
      <h1>Car Rental AI</h1>
      <p>Skeleton frontend - TV D: Le Viet Anh</p>

      {health && (
        <div style={{ marginTop: '1rem', padding: '1rem', background: '#d4edda', borderRadius: '4px' }}>
          <strong>API Health:</strong> {health.status} | Service: {health.service}
        </div>
      )}

      {error && (
        <div style={{ marginTop: '1rem', padding: '1rem', background: '#f8d7da', borderRadius: '4px' }}>
          <strong>Error:</strong> {error}
        </div>
      )}

      <p style={{ marginTop: '2rem', color: '#666' }}>
        Skeleton includes: React 18 + Vite + Router + API health check
      </p>
    </div>
  )
}

export default App
