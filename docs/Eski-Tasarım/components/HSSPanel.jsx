/* global React */
// ============================================================
// HSSPanel - Hava Savunma Sistemi + QR Kamikaze paneli
// TEKNOFEST /api/hss_koordinatlari ve /api/qr_koordinati
// ============================================================

const HSSPanel = ({ sim }) => {
  const [hssActive, setHssActive] = React.useState(true);
  const hssList = hssActive ? [
    { id: 0, lat: 40.23260, lng: 29.00573, r: 50 },
    { id: 1, lat: 40.23351, lng: 28.99976, r: 50 },
    { id: 2, lat: 40.23105, lng: 29.00744, r: 75 },
    { id: 3, lat: 40.23090, lng: 29.00221, r: 150 },
  ] : [];
  const qr = { lat: 41.51238882, lng: 36.11935778 };

  return (
    <div style={{display: 'flex', flexDirection: 'column', gap: 10}}>
      {/* HSS Bölümü */}
      <div className="hud-panel hud-corners" style={{padding: 12}}>
        <span className="corner-bl"/><span className="corner-br"/>
        <div style={{display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 10, paddingBottom: 8, borderBottom: '1px solid var(--border-dim)'}}>
          <span className="hud-label">▸ HAVA SAVUNMA SİSTEMİ</span>
          <span className={`hud-chip ${hssActive ? 'red' : ''}`}>
            {hssActive ? 'AKTİF' : 'BEKLEMEDE'}
          </span>
        </div>
        {!hssActive && (
          <div style={{fontSize: 10.5, color: 'var(--text-dim)', fontFamily: 'var(--font-mono)', padding: '8px 0'}}>
            Hakem duyurusu bekleniyor. Duyuru sonrası /api/hss_koordinatlari aktifleşecek.
          </div>
        )}
        {hssList.map(h => (
          <div key={h.id} style={{
            display: 'flex', justifyContent: 'space-between', padding: '6px 0',
            borderBottom: '1px solid var(--border-dim)', fontSize: 11, fontFamily: 'var(--font-mono)',
          }}>
            <span style={{color: 'var(--danger-bright)', fontWeight: 600}}>HSS-{String(h.id).padStart(2, '0')}</span>
            <span style={{color: '#ccc'}}>{h.lat.toFixed(4)}°, {h.lng.toFixed(4)}°</span>
            <span style={{color: 'var(--warn)'}}>⌀ {h.r}m</span>
          </div>
        ))}
        <button onClick={() => setHssActive(v => !v)} className="hud-btn ghost"
          style={{width: '100%', marginTop: 8, padding: '6px 0', fontSize: 10}}>
          ⟲ /api/hss_koordinatlari SORGULA
        </button>
      </div>

      {/* QR Kamikaze */}
      <div className="hud-panel hud-corners" style={{padding: 12}}>
        <span className="corner-bl"/><span className="corner-br"/>
        <div className="hud-label" style={{marginBottom: 10}}>▸ QR KAMİKAZE HEDEFİ</div>
        <div style={{display: 'grid', gridTemplateColumns: 'auto 1fr', gap: '4px 10px', fontFamily: 'var(--font-mono)', fontSize: 11}}>
          <span style={{color: 'var(--text-dim)'}}>LAT</span><span style={{color: '#fff'}}>{qr.lat.toFixed(7)}°N</span>
          <span style={{color: 'var(--text-dim)'}}>LNG</span><span style={{color: '#fff'}}>{qr.lng.toFixed(7)}°E</span>
          <span style={{color: 'var(--text-dim)'}}>DURUM</span><span style={{color: 'var(--warn)'}}>OKUNMADI</span>
        </div>
        <button onClick={() => sim.pushAlert('kamikaze', 'Kamikaze rotası planlandı - QR hedefine yönelim')} className="hud-btn primary"
          style={{width: '100%', marginTop: 10, padding: '8px 0', fontSize: 11}}>
          ✕ KAMİKAZE BAŞLAT
        </button>
      </div>

      {/* API Status */}
      <div className="hud-panel" style={{padding: 10, fontSize: 10, fontFamily: 'var(--font-mono)'}}>
        <div className="hud-label" style={{fontSize: 9, marginBottom: 6}}>▸ API DURUM</div>
        {[
          {n: '/api/giris', s: 'ok'},
          {n: '/api/sunucusaati', s: 'ok'},
          {n: '/api/telemetri_gonder', s: 'ok'},
          {n: '/api/kilitlenme_bilgisi', s: 'ready'},
          {n: '/api/kamikaze_bilgisi', s: 'ready'},
          {n: '/api/qr_koordinati', s: 'ok'},
          {n: '/api/hss_koordinatlari', s: hssActive ? 'ok' : 'wait'},
        ].map(a => (
          <div key={a.n} style={{display: 'flex', justifyContent: 'space-between', padding: '2px 0'}}>
            <span style={{color: '#aab'}}>{a.n}</span>
            <span style={{
              color: a.s === 'ok' ? 'var(--hud-green)' : a.s === 'ready' ? 'var(--accent-bright)' : 'var(--warn)',
              fontSize: 9, textTransform: 'uppercase',
            }}>● {a.s === 'ok' ? '200' : a.s === 'ready' ? 'HAZIR' : 'BEKLEMEDE'}</span>
          </div>
        ))}
      </div>
    </div>
  );
};

window.HSSPanel = HSSPanel;
