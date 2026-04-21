/* global React */
// ============================================================
// CameraScreen - KAMERA ana ekranı (Varyasyon 2)
// Tam ekran kamera + gun-camera HUD overlay'i
// ============================================================

const CameraScreen = () => {
  const sim = window.useSimulation();
  const [zoom, setZoom] = React.useState(4.5);
  const [recTime, setRecTime] = React.useState(0);

  React.useEffect(() => {
    const id = setInterval(() => setRecTime(t => t + 1), 1000);
    return () => clearInterval(id);
  }, []);

  const recFmt = `${String(Math.floor(recTime / 60)).padStart(2, '0')}:${String(recTime % 60).padStart(2, '0')}`;

  const handleAction = (a) => {
    if (a === 'ZOOM_IN') setZoom(z => Math.min(20, z + 0.5));
    else if (a === 'ZOOM_OUT') setZoom(z => Math.max(1, z - 0.5));
    else sim.handleAction(a);
  };

  return (
    <div style={{position: 'absolute', inset: 0, background: '#000'}}>
      <window.CameraHUD
        flight={sim.flight}
        target={sim.target}
        zoom={zoom}
        recording={recFmt}
        onAction={handleAction}
      />

      {/* Toast uyarılar kamera ekranında da */}
      <window.AlertStack alerts={sim.alerts} compact/>

      {/* Küçük harita (picture-in-picture - sol alt) */}
      <div style={{
        position: 'absolute', bottom: 90, left: 20, width: 240, height: 180,
        border: '1px solid var(--border-strong)',
        background: 'var(--bg-primary)',
        overflow: 'hidden',
        zIndex: 15,
      }}>
        <div className="hud-label" style={{
          position: 'absolute', top: 4, left: 6, zIndex: 2,
          background: 'rgba(0,0,0,0.6)', padding: '1px 4px', fontSize: 9,
        }}>▸ HARİTA</div>
        <window.MapView
          mapType="satellite"
          ownAircraft={sim.ownAircraft}
          otherAircraft={sim.otherAircraft}
          waypoints={sim.waypoints}
          onMapClick={() => {}}
          showGrid={false}
        />
      </div>

      {/* Küçük kontak listesi (sağ alt) */}
      <div style={{
        position: 'absolute', bottom: 90, right: 20, width: 220,
        maxHeight: 200, zIndex: 15,
      }}>
        <div className="hud-panel" style={{padding: '8px 10px'}}>
          <div className="hud-label" style={{fontSize: 9, marginBottom: 6}}>▸ TESPİT EDİLEN</div>
          {sim.otherAircraft.slice(0, 4).map(c => {
            const color = c.team === 'hostile' ? 'var(--danger-bright)' : c.team === 'friendly' ? 'var(--hud-green)' : 'var(--warn)';
            return (
              <div key={c.id} onClick={() => sim.setLockedId(c.id)}
                style={{
                  display: 'flex', justifyContent: 'space-between',
                  padding: '3px 0', fontSize: 10, fontFamily: 'var(--font-mono)',
                  cursor: 'pointer',
                  background: sim.lockedId === c.id ? 'rgba(255,46,76,0.15)' : 'transparent',
                }}>
                <span style={{color}}>● {c.id}</span>
                <span style={{color: '#ccc'}}>{c.range}m</span>
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
};

window.CameraScreen = CameraScreen;
