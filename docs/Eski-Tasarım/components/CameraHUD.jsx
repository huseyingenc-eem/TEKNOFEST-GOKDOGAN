/* global React */
// ============================================================
// CameraHUD - Tam ekran kamera üstü HUD overlay'i
// Gun-camera / FPV tarzı, hedef kilitleme, kripleri, ranging
// ============================================================

const CameraHUD = ({ flight, target, zoom, recording, onAction }) => {
  const { pitch, roll, heading, airspeed, altitude } = flight;

  return (
    <div style={chudStyles.wrap}>
      {/* SCAN LINES */}
      <div className="scanlines" style={{position: 'absolute', inset: 0}}/>

      {/* Fake video görüntüsü - mock sahne */}
      <CameraFeed/>

      {/* Üst bilgi çubuğu */}
      <div style={chudStyles.topBar}>
        <div style={chudStyles.topGroup}>
          <span className="mono" style={{color: 'var(--danger-bright)', fontSize: 11, letterSpacing: '0.2em'}} className="blink">
            ● REC {recording}
          </span>
          <span className="hud-label-dim">CAM-01 · EO/IR</span>
          <span className="hud-chip green">ONLINE</span>
        </div>
        <div style={chudStyles.topGroup}>
          <span className="mono" style={{color: 'var(--accent-bright)', fontSize: 12}}>ZOOM {zoom.toFixed(1)}x</span>
          <span className="hud-label-dim">FOV 24°</span>
          <span className="mono" style={{color: 'var(--accent-bright)', fontSize: 12}}>EO</span>
        </div>
      </div>

      {/* Sol - telemetri (küçük) */}
      <div style={chudStyles.leftStack}>
        <MiniStack label="IAS" value={Math.round(airspeed)} unit="M/S"/>
        <MiniStack label="ALT" value={Math.round(altitude)} unit="M"/>
        <MiniStack label="HDG" value={String(Math.round(heading)).padStart(3, '0')} unit="°"/>
      </div>

      {/* Sağ - hedef bilgisi */}
      {target && (
        <div style={chudStyles.rightStack}>
          <div className="hud-panel" style={{padding: '8px 10px', borderColor: 'var(--danger)', background: 'rgba(255,46,76,0.08)'}}>
            <div className="hud-label" style={{color: 'var(--danger-bright)', fontSize: 9}}>▸ HEDEF KİLİTLENDİ</div>
            <div style={{display: 'grid', gridTemplateColumns: 'auto 1fr', gap: '2px 10px', marginTop: 6, fontSize: 11, fontFamily: 'var(--font-mono)'}}>
              <span style={{color: 'var(--text-dim)'}}>ID</span><span style={{color: '#fff'}}>{target.id}</span>
              <span style={{color: 'var(--text-dim)'}}>TİP</span><span style={{color: '#fff'}}>{target.type}</span>
              <span style={{color: 'var(--text-dim)'}}>MESAFE</span><span style={{color: 'var(--danger-bright)'}}>{target.range}m</span>
              <span style={{color: 'var(--text-dim)'}}>KONFIDANS</span><span style={{color: 'var(--hud-green)'}}>{target.confidence}%</span>
            </div>
          </div>
        </div>
      )}

      {/* Merkez crosshair */}
      <Crosshair target={target}/>

      {/* Alt çubuk */}
      <div style={chudStyles.bottomBar}>
        <button className="hud-btn ghost" onClick={() => onAction('ZOOM_OUT')} style={{padding: '8px 14px'}}>ZOOM -</button>
        <button className="hud-btn ghost" onClick={() => onAction('EO_IR')} style={{padding: '8px 14px'}}>EO ⟷ IR</button>
        <button
          className={`hud-btn ${target ? 'danger' : ''}`}
          onClick={() => onAction(target ? 'UNLOCK' : 'LOCK')}
          style={{padding: '8px 20px', letterSpacing: '0.15em'}}
        >
          {target ? '✕ KİLİDİ BIRAK' : '◎ KİLİTLE'}
        </button>
        <button className="hud-btn ghost" onClick={() => onAction('SNAPSHOT')} style={{padding: '8px 14px'}}>📷 SNAPSHOT</button>
        <button className="hud-btn ghost" onClick={() => onAction('ZOOM_IN')} style={{padding: '8px 14px'}}>ZOOM +</button>
      </div>

      {/* Pitch ladder (kamera üstünde) */}
      <PitchLadder pitch={pitch} roll={roll}/>

      {/* Köşe sensör barları */}
      <div style={chudStyles.cornerTR}>
        <TinyBar label="LZR" value={76}/>
        <TinyBar label="GMB" value={91}/>
      </div>
    </div>
  );
};

const Crosshair = ({ target }) => (
  <div style={{
    position: 'absolute', top: '50%', left: '50%',
    transform: 'translate(-50%, -50%)', pointerEvents: 'none',
    width: target ? 160 : 100, height: target ? 160 : 100,
    transition: 'all 0.3s',
  }}>
    {target && (
      <>
        {/* Lock brackets */}
        {['tl', 'tr', 'bl', 'br'].map(pos => (
          <span key={pos} style={{
            position: 'absolute',
            [pos[0] === 't' ? 'top' : 'bottom']: 0,
            [pos[1] === 'l' ? 'left' : 'right']: 0,
            width: 22, height: 22,
            [pos[0] === 't' ? 'borderTop' : 'borderBottom']: '2px solid var(--danger)',
            [pos[1] === 'l' ? 'borderLeft' : 'borderRight']: '2px solid var(--danger)',
            boxShadow: '0 0 8px var(--danger)',
          }}/>
        ))}
      </>
    )}
    <svg viewBox="0 0 100 100" style={{position: 'absolute', inset: 0}}>
      <g stroke={target ? 'var(--danger-bright)' : 'var(--accent-bright)'} strokeWidth="1" fill="none" filter="drop-shadow(0 0 2px currentColor)">
        <line x1="50" y1="20" x2="50" y2="42"/>
        <line x1="50" y1="58" x2="50" y2="80"/>
        <line x1="20" y1="50" x2="42" y2="50"/>
        <line x1="58" y1="50" x2="80" y2="50"/>
        <circle cx="50" cy="50" r="3" fill={target ? 'var(--danger)' : 'none'}/>
        {!target && <circle cx="50" cy="50" r="14" opacity="0.5"/>}
      </g>
    </svg>
  </div>
);

const PitchLadder = ({ pitch, roll }) => (
  <div style={{
    position: 'absolute', inset: 0, pointerEvents: 'none',
    display: 'grid', placeItems: 'center',
  }}>
    <div style={{
      width: 400, height: 300, position: 'relative',
      transform: `rotate(${-roll}deg)`,
    }}>
      <div style={{
        position: 'absolute', top: '50%', left: 0, right: 0,
        transform: `translateY(${pitch * 3}px)`,
      }}>
        {[-30, -20, -10, 10, 20, 30].map(p => (
          <div key={p} style={{
            position: 'absolute', top: `${-p * 3}px`,
            left: '50%', transform: 'translateX(-50%)',
            width: Math.abs(p) % 20 === 0 ? 140 : 70, height: 1,
            display: 'flex', justifyContent: 'space-between', alignItems: 'center',
          }}>
            <div style={{width: 30, height: 1, background: 'var(--accent-bright)', opacity: 0.7}}/>
            <span style={{fontSize: 10, color: 'var(--accent-bright)', fontFamily: 'var(--font-mono)', opacity: 0.8}}>
              {Math.abs(p)}
            </span>
            <div style={{width: 30, height: 1, background: 'var(--accent-bright)', opacity: 0.7}}/>
          </div>
        ))}
        {/* Horizon line */}
        <div style={{position: 'absolute', top: 0, left: '20%', right: '20%', height: 1, background: 'var(--accent-bright)', opacity: 0.5, boxShadow: '0 0 4px var(--accent)'}}/>
      </div>
    </div>
  </div>
);

const CameraFeed = () => (
  <div style={{position: 'absolute', inset: 0, overflow: 'hidden'}}>
    {/* Mock aerial view */}
    <div style={{
      position: 'absolute', inset: 0,
      background: `
        radial-gradient(ellipse at 40% 60%, #3a4a2a 0%, transparent 40%),
        radial-gradient(ellipse at 70% 30%, #4a3a2a 0%, transparent 50%),
        linear-gradient(180deg, #1a2018 0%, #2a2e22 70%, #1a1814 100%)
      `,
    }}/>
    <svg style={{position: 'absolute', inset: 0, width: '100%', height: '100%'}} viewBox="0 0 100 60" preserveAspectRatio="none">
      {/* Field patches */}
      <path d="M 10 30 L 35 25 L 50 40 L 25 45 Z" fill="#4a5a2e" opacity="0.6"/>
      <path d="M 40 20 L 70 18 L 75 35 L 50 40 Z" fill="#5a4a2a" opacity="0.6"/>
      <path d="M 65 30 L 90 28 L 95 50 L 70 52 Z" fill="#3a4a2e" opacity="0.5"/>
      {/* Roads */}
      <path d="M 0 45 Q 30 42 60 48 T 100 50" stroke="#5a5048" strokeWidth="0.6" fill="none" opacity="0.8"/>
      <path d="M 20 10 L 45 40 L 80 55" stroke="#4a4038" strokeWidth="0.4" fill="none" opacity="0.6"/>
      {/* Buildings/structures */}
      {[[22,35,2,1.5],[28,37,1.5,1],[62,25,3,2],[65,29,2,1.5],[82,42,2.5,1.8],[15,20,1.5,1],[48,32,1.8,1.2]].map(([x,y,w,h], i) => (
        <rect key={i} x={x} y={y} width={w} height={h} fill="#6a5a48" opacity="0.7"/>
      ))}
      {/* Target position marker (truck) */}
      <g transform="translate(52, 36)">
        <rect x="-2" y="-1" width="4" height="2" fill="#2a1e14"/>
        <rect x="-2" y="-0.7" width="1.2" height="1.4" fill="#3a2a1a"/>
      </g>
    </svg>
    {/* Slight chromatic vignette */}
    <div style={{
      position: 'absolute', inset: 0,
      background: 'radial-gradient(ellipse at center, transparent 40%, rgba(0,0,0,0.55) 100%)',
      pointerEvents: 'none',
    }}/>
  </div>
);

const MiniStack = ({ label, value, unit }) => (
  <div className="hud-panel" style={{padding: '6px 10px', minWidth: 78}}>
    <div className="hud-label-dim" style={{fontSize: 9}}>{label}</div>
    <div style={{display: 'baseline', display: 'flex', alignItems: 'baseline', gap: 2}}>
      <span className="display tabular" style={{fontSize: 20, fontWeight: 700, color: 'var(--accent-bright)', lineHeight: 1}}>{value}</span>
      <span style={{fontSize: 9, color: 'var(--text-secondary)', fontFamily: 'var(--font-mono)'}}>{unit}</span>
    </div>
  </div>
);

const TinyBar = ({ label, value }) => (
  <div style={{display: 'flex', alignItems: 'center', gap: 6, fontSize: 10, fontFamily: 'var(--font-mono)'}}>
    <span style={{color: 'var(--text-dim)', width: 26}}>{label}</span>
    <div style={{width: 50, height: 3, background: 'rgba(255,255,255,0.06)', position: 'relative'}}>
      <div style={{position: 'absolute', inset: 0, right: `${100 - value}%`, background: 'var(--accent)'}}/>
    </div>
    <span style={{color: 'var(--accent-bright)', width: 22, textAlign: 'right'}}>{value}</span>
  </div>
);

const chudStyles = {
  wrap: { position: 'absolute', inset: 0, overflow: 'hidden', background: '#000', fontFamily: 'var(--font-mono)' },
  topBar: {
    position: 'absolute', top: 16, left: 16, right: 16,
    display: 'flex', justifyContent: 'space-between', alignItems: 'center',
    zIndex: 10,
  },
  topGroup: { display: 'flex', gap: 14, alignItems: 'center' },
  leftStack: {
    position: 'absolute', top: '50%', left: 20, transform: 'translateY(-50%)',
    display: 'flex', flexDirection: 'column', gap: 8, zIndex: 10,
  },
  rightStack: {
    position: 'absolute', top: '50%', right: 20, transform: 'translateY(-50%)',
    display: 'flex', flexDirection: 'column', gap: 8, zIndex: 10, width: 200,
  },
  bottomBar: {
    position: 'absolute', bottom: 20, left: '50%', transform: 'translateX(-50%)',
    display: 'flex', gap: 8, zIndex: 10,
  },
  cornerTR: {
    position: 'absolute', top: 54, right: 16,
    display: 'flex', flexDirection: 'column', gap: 4, zIndex: 10,
  },
};

window.CameraHUD = CameraHUD;
