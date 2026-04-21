/* global React */
// ============================================================
// MapView - Harita bileşeni (mock)
// Uydu/topografik toggle, İHA konumu, diğer takımlar, waypointler
// ============================================================

const MapView = ({ mapType, ownAircraft, otherAircraft, waypoints, onMapClick, showGrid = true }) => {
  // Harita koordinat sistemi (0-1 normalized)
  const project = (lat, lng) => {
    // Mock: center Istanbul
    const centerLat = 41.0, centerLng = 29.0;
    const scale = 0.08;
    return {
      x: ((lng - centerLng) / scale + 1) / 2 * 100,
      y: (1 - (lat - centerLat) / scale - 1) / -2 * 100 + 50,
    };
  };

  return (
    <div style={{...mapStyles.wrap, background: mapType === 'satellite' ? '#0a1420' : '#0f0d08'}}>
      {/* Harita arka planı */}
      {mapType === 'satellite' ? <SatelliteBackground/> : <TopoBackground/>}

      {/* Grid overlay */}
      {showGrid && <div style={mapStyles.gridOverlay}/>}

      {/* Waypoint çizgileri */}
      <svg style={mapStyles.svgLayer} viewBox="0 0 100 100" preserveAspectRatio="none">
        {waypoints.length > 1 && (
          <polyline
            points={waypoints.map(wp => {
              const p = project(wp.lat, wp.lng);
              return `${p.x},${p.y}`;
            }).join(' ')}
            fill="none" stroke="#ff6b1a" strokeWidth="0.25" strokeDasharray="1 0.6"
            opacity="0.9" vectorEffect="non-scaling-stroke"
            style={{strokeWidth: 2}}
          />
        )}
        {/* İHA kat ettiği yol */}
        {ownAircraft.trail && ownAircraft.trail.length > 1 && (
          <polyline
            points={ownAircraft.trail.map(p => {
              const pr = project(p.lat, p.lng);
              return `${pr.x},${pr.y}`;
            }).join(' ')}
            fill="none" stroke="#39ff7a" strokeWidth="0.2"
            opacity="0.6" vectorEffect="non-scaling-stroke"
            style={{strokeWidth: 1.5}}
          />
        )}
      </svg>

      {/* Waypoint'ler */}
      {waypoints.map((wp, i) => {
        const p = project(wp.lat, wp.lng);
        return <WaypointMarker key={i} x={p.x} y={p.y} idx={i + 1} type={wp.type}/>;
      })}

      {/* Diğer takımların İHA'ları */}
      {otherAircraft.map((ac, i) => {
        const p = project(ac.lat, ac.lng);
        return <AircraftMarker key={ac.id} x={p.x} y={p.y} aircraft={ac}/>;
      })}

      {/* Kendi İHA'mız */}
      {(() => {
        const p = project(ownAircraft.lat, ownAircraft.lng);
        return <OwnAircraftMarker x={p.x} y={p.y} heading={ownAircraft.heading}/>;
      })()}

      {/* Map tıklama alanı */}
      <div style={mapStyles.clickLayer} onClick={onMapClick}/>
    </div>
  );
};

const OwnAircraftMarker = ({ x, y, heading }) => (
  <div style={{
    position: 'absolute', left: `${x}%`, top: `${y}%`,
    transform: `translate(-50%, -50%)`, zIndex: 10, pointerEvents: 'none',
  }}>
    {/* Pulse ring */}
    <div style={{
      position: 'absolute', inset: -20,
      border: '2px solid #ff6b1a', borderRadius: '50%',
      opacity: 0.35,
      animation: 'mapPulse 2s infinite',
    }}/>
    <style>{`
      @keyframes mapPulse {
        0% { transform: scale(0.8); opacity: 0.7; }
        100% { transform: scale(1.8); opacity: 0; }
      }
    `}</style>
    <svg width="40" height="40" viewBox="0 0 40 40" style={{transform: `rotate(${heading}deg)`}}>
      <defs>
        <filter id="glow-self">
          <feGaussianBlur stdDeviation="1.5"/>
        </filter>
      </defs>
      <g filter="url(#glow-self)">
        <path d="M20 5 L28 30 L20 25 L12 30 Z" fill="#ff6b1a" opacity="0.4"/>
      </g>
      <path d="M20 5 L28 30 L20 25 L12 30 Z" fill="#ff6b1a" stroke="#fff" strokeWidth="1"/>
      <circle cx="20" cy="22" r="1.5" fill="#fff"/>
    </svg>
    {/* Etiket */}
    <div style={{
      position: 'absolute', top: '100%', left: '50%', transform: 'translateX(-50%)',
      marginTop: 4, padding: '2px 6px', background: 'rgba(255, 107, 26, 0.9)',
      color: '#0b0d11', fontSize: 9, fontFamily: 'var(--font-mono)', fontWeight: 700,
      whiteSpace: 'nowrap', letterSpacing: '0.08em',
    }}>GÖKDOĞAN-01</div>
  </div>
);

const AircraftMarker = ({ x, y, aircraft }) => {
  const color = aircraft.team === 'hostile' ? '#ff2e4c' : aircraft.team === 'friendly' ? '#39ff7a' : '#ffb800';
  const isLocked = aircraft.locked;
  return (
    <div style={{
      position: 'absolute', left: `${x}%`, top: `${y}%`,
      transform: 'translate(-50%, -50%)', zIndex: 8, pointerEvents: 'none',
    }}>
      {isLocked && (
        <div style={{
          position: 'absolute', inset: -18,
          border: `1.5px solid ${color}`,
          animation: 'lockPulse 0.8s infinite',
        }}>
          <span style={{position: 'absolute', top: -2, left: -2, width: 6, height: 6, borderTop: `2px solid ${color}`, borderLeft: `2px solid ${color}`}}/>
          <span style={{position: 'absolute', top: -2, right: -2, width: 6, height: 6, borderTop: `2px solid ${color}`, borderRight: `2px solid ${color}`}}/>
          <span style={{position: 'absolute', bottom: -2, left: -2, width: 6, height: 6, borderBottom: `2px solid ${color}`, borderLeft: `2px solid ${color}`}}/>
          <span style={{position: 'absolute', bottom: -2, right: -2, width: 6, height: 6, borderBottom: `2px solid ${color}`, borderRight: `2px solid ${color}`}}/>
        </div>
      )}
      <style>{`@keyframes lockPulse { 0%,100% {opacity:1} 50% {opacity:0.3} }`}</style>
      <svg width="26" height="26" viewBox="0 0 26 26" style={{transform: `rotate(${aircraft.heading}deg)`}}>
        <path d="M13 3 L19 20 L13 16 L7 20 Z" fill="none" stroke={color} strokeWidth="1.5"/>
      </svg>
      <div style={{
        position: 'absolute', top: '100%', left: '50%', transform: 'translateX(-50%)',
        marginTop: 2, padding: '1px 4px', background: 'rgba(0,0,0,0.7)',
        color, fontSize: 8.5, fontFamily: 'var(--font-mono)', fontWeight: 600,
        whiteSpace: 'nowrap', letterSpacing: '0.06em',
        border: `1px solid ${color}`,
      }}>{aircraft.id} · {aircraft.alt}m</div>
    </div>
  );
};

const WaypointMarker = ({ x, y, idx, type }) => {
  const color = type === 'takeoff' ? '#39ff7a' : type === 'land' ? '#ff2e4c' : '#ff6b1a';
  return (
    <div style={{
      position: 'absolute', left: `${x}%`, top: `${y}%`,
      transform: 'translate(-50%, -50%)', zIndex: 7,
    }}>
      <div style={{
        width: 20, height: 20, borderRadius: '50%',
        background: 'rgba(13,16,20,0.85)', border: `2px solid ${color}`,
        display: 'grid', placeItems: 'center',
        color, fontSize: 10, fontFamily: 'var(--font-mono)', fontWeight: 700,
      }}>{idx}</div>
    </div>
  );
};

// ---- Uydu arka plan (mock) ----
const SatelliteBackground = () => (
  <div style={{position: 'absolute', inset: 0, overflow: 'hidden'}}>
    <div style={{
      position: 'absolute', inset: 0,
      background: `
        radial-gradient(ellipse at 30% 40%, #1a3550 0%, transparent 45%),
        radial-gradient(ellipse at 70% 60%, #0f2438 0%, transparent 50%),
        radial-gradient(ellipse at 50% 20%, #243748 0%, transparent 40%),
        #081420
      `,
    }}/>
    {/* Kara/deniz alanları */}
    <svg style={{position: 'absolute', inset: 0, width: '100%', height: '100%'}} viewBox="0 0 100 100" preserveAspectRatio="none">
      <path d="M 0 30 Q 20 25 35 35 T 65 40 Q 80 45 100 42 L 100 100 L 0 100 Z" fill="#1a2432" opacity="0.7"/>
      <path d="M 10 50 Q 25 48 40 55 T 70 58 Q 85 62 100 60 L 100 100 L 0 100 L 0 55 Z" fill="#222f3f" opacity="0.5"/>
      <path d="M 0 70 Q 25 68 45 72 T 80 75 L 100 74 L 100 100 L 0 100 Z" fill="#2a3847" opacity="0.5"/>
      {/* Statik düşük yoğunluklu noktalar */}
      {/* Yollar */}
      <path d="M 15 35 L 45 50 L 60 65 L 85 80" stroke="#3a4656" strokeWidth="0.3" fill="none" opacity="0.6"/>
      <path d="M 5 60 L 35 58 L 55 72 L 95 75" stroke="#3a4656" strokeWidth="0.25" fill="none" opacity="0.5"/>
    </svg>
  </div>
);

// ---- Topo arka plan (mock) ----
const TopoBackground = () => (
  <div style={{position: 'absolute', inset: 0, overflow: 'hidden', background: '#0f0d08'}}>
    <svg style={{position: 'absolute', inset: 0, width: '100%', height: '100%'}} viewBox="0 0 100 100" preserveAspectRatio="none">
      {/* Kontur çizgileri */}
      {[20, 35, 50, 65, 80].map((y, i) => (
        <path key={i}
          d={`M 0 ${y} Q 20 ${y - 8 + i*2} 40 ${y - 3} T 80 ${y + 2} T 100 ${y}`}
          fill="none" stroke="#3a2f1a" strokeWidth="0.15" opacity="0.7"
        />
      ))}
      {[25, 40, 55, 70].map((y, i) => (
        <path key={`b-${i}`}
          d={`M 0 ${y+3} Q 25 ${y} 50 ${y+1} T 100 ${y+2}`}
          fill="none" stroke="#2d2414" strokeWidth="0.1" opacity="0.5"
        />
      ))}
      {/* Nehir */}
      <path d="M 10 20 Q 30 40 40 55 T 55 85" fill="none" stroke="#1a4a6a" strokeWidth="0.8" opacity="0.7"/>
      {/* Yollar */}
      <path d="M 5 45 L 30 48 L 55 60 L 95 62" stroke="#5a4a2a" strokeWidth="0.3" fill="none" strokeDasharray="1 0.5"/>
    </svg>
  </div>
);

const mapStyles = {
  wrap: { position: 'absolute', inset: 0, overflow: 'hidden' },
  gridOverlay: {
    position: 'absolute', inset: 0, pointerEvents: 'none',
    backgroundImage: 'linear-gradient(rgba(255,107,26,0.05) 1px, transparent 1px), linear-gradient(90deg, rgba(255,107,26,0.05) 1px, transparent 1px)',
    backgroundSize: '50px 50px',
  },
  svgLayer: { position: 'absolute', inset: 0, width: '100%', height: '100%', pointerEvents: 'none' },
  clickLayer: { position: 'absolute', inset: 0, cursor: 'crosshair' },
};

window.MapView = MapView;
