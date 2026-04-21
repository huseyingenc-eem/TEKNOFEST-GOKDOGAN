/* global React */
// ============================================================
// EmergencyControls - Acil durum butonları
// ARM/DISARM, RTL (Return to Launch), LAND, LOITER
// ============================================================

const EmergencyControls = ({ state, onAction }) => {
  const { armed, mode } = state;

  return (
    <div className="hud-panel hud-corners" style={ecStyles.wrap}>
      <span className="corner-bl"/><span className="corner-br"/>
      <div style={ecStyles.header}>
        <span className="hud-label">▸ KOMUT MERKEZİ</span>
        <span className={`hud-chip ${armed ? 'green' : 'red'}`}>
          <span style={{width: 6, height: 6, borderRadius: '50%', background: 'currentColor'}} className={armed ? 'pulse' : ''}/>
          {armed ? 'ARMED' : 'DISARMED'}
        </span>
      </div>

      <div style={ecStyles.modeRow}>
        <span className="hud-label-dim">MODE</span>
        <span className="display" style={{fontSize: 16, fontWeight: 600, color: 'var(--accent-bright)', letterSpacing: '0.1em'}}>
          {mode}
        </span>
      </div>

      <div style={ecStyles.grid}>
        <button
          className={`hud-btn ${armed ? 'danger' : 'primary'}`}
          onClick={() => onAction(armed ? 'DISARM' : 'ARM')}
          style={{width: '100%', padding: '11px 0'}}
        >
          {armed ? 'DISARM' : 'ARM'}
        </button>
        <button className="hud-btn" onClick={() => onAction('TAKEOFF')} style={{width: '100%', padding: '11px 0'}}>
          KALKIŞ
        </button>
        <button className="hud-btn" onClick={() => onAction('LOITER')} style={{width: '100%', padding: '11px 0'}}>
          LOITER
        </button>
        <button className="hud-btn" onClick={() => onAction('AUTO')} style={{width: '100%', padding: '11px 0'}}>
          AUTO
        </button>
      </div>

      <div style={{display: 'flex', gap: 6, marginTop: 8}}>
        <button className="hud-btn danger" onClick={() => onAction('RTL')} style={{flex: 1, padding: '12px 0'}}>
          ↩ RTL
        </button>
        <button className="hud-btn danger" onClick={() => onAction('LAND')} style={{flex: 1, padding: '12px 0'}}>
          ▼ LAND
        </button>
      </div>

      <button
        className="hud-btn danger"
        onClick={() => onAction('KILL')}
        style={{width: '100%', padding: '10px 0', marginTop: 6, letterSpacing: '0.2em', fontSize: 12}}
      >
        ⚠ ACİL DURDUR
      </button>
    </div>
  );
};

const ecStyles = {
  wrap: { width: 260, padding: 12 },
  header: { display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 10, paddingBottom: 8, borderBottom: '1px solid var(--border-dim)' },
  modeRow: { display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '6px 10px', background: 'rgba(255,107,26,0.05)', border: '1px solid var(--border-dim)', marginBottom: 10 },
  grid: { display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 6 },
};

window.EmergencyControls = EmergencyControls;
