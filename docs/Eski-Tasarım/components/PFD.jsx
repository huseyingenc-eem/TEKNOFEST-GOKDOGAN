/* global React */
// ============================================================
// PFD - Primary Flight Display (Birincil Uçuş Göstergesi)
// Suni ufuk + hız şeridi + irtifa şeridi + pusula
// ============================================================

const PFD = ({ flight, compact = false }) => {
  const { pitch, roll, heading, airspeed, altitude, vspeed } = flight;
  const size = compact ? 200 : 260;

  return (
    <div className="hud-panel hud-corners" style={{...pfdStyles.wrap, width: compact ? 340 : 420}}>
      <span className="corner-bl"/><span className="corner-br"/>
      <div style={pfdStyles.header}>
        <span className="hud-label">▸ PFD</span>
        <span className="hud-label-dim">ATTITUDE · ALT · SPD</span>
      </div>

      <div style={{display: 'flex', gap: 8, padding: 10}}>
        {/* Airspeed tape */}
        <TapeStrip
          label="IAS"
          value={airspeed}
          unit="m/s"
          range={20}
          step={5}
          height={size}
          accent="var(--accent)"
        />

        {/* Artificial Horizon */}
        <ArtificialHorizon pitch={pitch} roll={roll} size={size}/>

        {/* Altitude tape */}
        <TapeStrip
          label="ALT"
          value={altitude}
          unit="m"
          range={100}
          step={25}
          height={size}
          accent="var(--hud-green)"
          right
        />
      </div>

      {/* Heading + VS */}
      <div style={pfdStyles.footer}>
        <div style={pfdStyles.footerItem}>
          <span className="hud-label-dim">HDG</span>
          <span className="display tabular" style={{fontSize: 20, color: 'var(--accent-bright)', fontWeight: 600}}>
            {String(Math.round(heading)).padStart(3, '0')}°
          </span>
        </div>
        <HeadingStrip heading={heading}/>
        <div style={pfdStyles.footerItem}>
          <span className="hud-label-dim">V/S</span>
          <span className="display tabular" style={{fontSize: 20, color: vspeed >= 0 ? 'var(--hud-green)' : 'var(--warn)', fontWeight: 600}}>
            {vspeed >= 0 ? '+' : ''}{vspeed.toFixed(1)}
          </span>
        </div>
      </div>
    </div>
  );
};

const ArtificialHorizon = ({ pitch, roll, size }) => {
  const pitchOffset = pitch * 4; // px per degree

  return (
    <div style={{
      width: size, height: size, position: 'relative',
      overflow: 'hidden', border: '1px solid var(--border-strong)',
      background: '#050709',
    }}>
      {/* Rolling scene */}
      <div style={{
        position: 'absolute', inset: -20,
        transform: `rotate(${-roll}deg)`,
        transformOrigin: 'center',
      }}>
        {/* Sky */}
        <div style={{
          position: 'absolute', inset: 0,
          background: 'linear-gradient(180deg, #1a3550 0%, #0e1e30 50%, #2d1a0c 50%, #1a0e08 100%)',
          transform: `translateY(${pitchOffset}px)`,
        }}>
          {/* Horizon line */}
          <div style={{position: 'absolute', top: '50%', left: 0, right: 0, height: 2, background: 'var(--accent-bright)', boxShadow: '0 0 8px var(--accent)'}}/>

          {/* Pitch ladder */}
          {[-30, -20, -10, 10, 20, 30].map(p => (
            <div key={p} style={{
              position: 'absolute', top: `calc(50% + ${-p * 4}px)`,
              left: '50%', transform: 'translateX(-50%)',
              width: Math.abs(p) % 20 === 0 ? 60 : 30, height: 1,
              background: 'var(--accent)', opacity: 0.7,
            }}>
              <span style={{
                position: 'absolute', right: -22, top: -7,
                fontSize: 10, color: 'var(--accent)', fontFamily: 'var(--font-mono)',
              }}>{Math.abs(p)}</span>
              <span style={{
                position: 'absolute', left: -22, top: -7,
                fontSize: 10, color: 'var(--accent)', fontFamily: 'var(--font-mono)',
              }}>{Math.abs(p)}</span>
            </div>
          ))}
        </div>
      </div>

      {/* Fixed aircraft symbol */}
      <svg style={{position: 'absolute', inset: 0, pointerEvents: 'none'}} viewBox={`0 0 ${size} ${size}`}>
        <g stroke="#ff6b1a" strokeWidth="2.5" fill="none">
          <line x1={size/2 - 50} y1={size/2} x2={size/2 - 15} y2={size/2}/>
          <line x1={size/2 + 15} y1={size/2} x2={size/2 + 50} y2={size/2}/>
          <line x1={size/2 - 15} y1={size/2} x2={size/2 - 15} y2={size/2 + 8}/>
          <line x1={size/2 + 15} y1={size/2} x2={size/2 + 15} y2={size/2 + 8}/>
          <circle cx={size/2} cy={size/2} r="3" fill="#ff6b1a"/>
        </g>
        {/* Roll indicator arc */}
        <g stroke="var(--accent)" strokeWidth="1" fill="none" opacity="0.6">
          <path d={`M ${size/2 - 70} ${size/2 - 50} A 85 85 0 0 1 ${size/2 + 70} ${size/2 - 50}`}/>
          {[-30, -20, -10, 0, 10, 20, 30].map(a => {
            const rad = (a - 90) * Math.PI / 180;
            const r1 = 85, r2 = a === 0 ? 78 : 82;
            const cx = size/2, cy = size/2;
            return <line key={a}
              x1={cx + r1 * Math.cos(rad)} y1={cy + r1 * Math.sin(rad)}
              x2={cx + r2 * Math.cos(rad)} y2={cy + r2 * Math.sin(rad)}
              strokeWidth={a === 0 ? 2 : 1}
            />;
          })}
        </g>
        {/* Roll pointer */}
        <g transform={`rotate(${-roll} ${size/2} ${size/2})`}>
          <polygon points={`${size/2 - 5},${size/2 - 75} ${size/2 + 5},${size/2 - 75} ${size/2},${size/2 - 68}`} fill="var(--accent-bright)"/>
        </g>
      </svg>
    </div>
  );
};

const TapeStrip = ({ label, value, unit, range, step, height, accent, right }) => {
  const marks = [];
  const startVal = Math.floor((value - range) / step) * step;
  for (let v = startVal; v <= value + range; v += step) {
    marks.push(v);
  }

  return (
    <div style={{width: 58, height, position: 'relative', border: '1px solid var(--border-strong)', background: '#050709', overflow: 'hidden'}}>
      <div style={{position: 'absolute', top: 4, left: 0, right: 0, textAlign: 'center', zIndex: 2}}>
        <span className="hud-label-dim" style={{fontSize: 9}}>{label}</span>
      </div>
      {/* Tape */}
      <div style={{position: 'absolute', top: '50%', left: 0, right: 0}}>
        {marks.map(m => {
          const offset = (m - value) * (height / (range * 2));
          return (
            <div key={m} style={{
              position: 'absolute', top: -offset, left: 0, right: 0,
              height: 1, display: 'flex', alignItems: 'center',
              [right ? 'flexDirection' : 'flexDirection']: 'row',
              justifyContent: right ? 'flex-start' : 'flex-end',
              paddingLeft: right ? 10 : 0, paddingRight: right ? 0 : 10,
            }}>
              <span style={{
                fontSize: 10, color: accent, fontFamily: 'var(--font-mono)',
                opacity: 0.8,
              }}>{m}</span>
              <div style={{width: 6, height: 1, background: accent, marginLeft: right ? -6 : 4, marginRight: right ? 4 : 0, order: right ? -1 : 1}}/>
            </div>
          );
        })}
      </div>
      {/* Center pointer */}
      <div style={{
        position: 'absolute', top: '50%', left: 0, right: 0,
        transform: 'translateY(-50%)',
        background: accent, height: 24, display: 'flex', alignItems: 'center',
        justifyContent: 'center', boxShadow: `0 0 8px ${accent}`,
        clipPath: right
          ? 'polygon(8px 0, 100% 0, 100% 100%, 8px 100%, 0 50%)'
          : 'polygon(0 0, calc(100% - 8px) 0, 100% 50%, calc(100% - 8px) 100%, 0 100%)',
      }}>
        <span style={{
          fontFamily: 'var(--font-display)', fontSize: 15, fontWeight: 700,
          color: '#0b0d11', fontVariantNumeric: 'tabular-nums'
        }}>{Math.round(value)}</span>
      </div>
      <div style={{position: 'absolute', bottom: 4, left: 0, right: 0, textAlign: 'center'}}>
        <span className="hud-label-dim" style={{fontSize: 9}}>{unit}</span>
      </div>
    </div>
  );
};

const HeadingStrip = ({ heading }) => {
  const marks = [];
  for (let i = -40; i <= 40; i += 5) {
    marks.push((heading + i + 360) % 360);
  }
  return (
    <div style={{flex: 1, height: 38, position: 'relative', border: '1px solid var(--border-strong)', background: '#050709', overflow: 'hidden'}}>
      {marks.map((m, idx) => {
        const left = ((idx - 8) * (100 / 17)) + 50;
        const major = m % 30 === 0;
        return (
          <div key={idx} style={{
            position: 'absolute', left: `${left}%`, top: 0, bottom: 0,
            width: 1, display: 'flex', flexDirection: 'column', alignItems: 'center',
            transform: 'translateX(-50%)',
          }}>
            <div style={{width: 1, height: major ? 10 : 5, background: 'var(--accent)'}}/>
            {major && <span style={{fontSize: 9, color: 'var(--accent-bright)', fontFamily: 'var(--font-mono)', marginTop: 2}}>
              {Math.round(m) === 0 ? 'N' : Math.round(m) === 90 ? 'E' : Math.round(m) === 180 ? 'S' : Math.round(m) === 270 ? 'W' : Math.round(m / 10)}
            </span>}
          </div>
        );
      })}
      <div style={{
        position: 'absolute', top: 0, left: '50%', transform: 'translateX(-50%)',
        width: 0, height: 0, borderLeft: '5px solid transparent', borderRight: '5px solid transparent', borderTop: '7px solid var(--accent-bright)',
      }}/>
    </div>
  );
};

const pfdStyles = {
  wrap: { padding: 0 },
  header: { display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '10px 14px 8px', borderBottom: '1px solid var(--border-dim)' },
  footer: { display: 'flex', alignItems: 'center', gap: 8, padding: '8px 12px 12px', borderTop: '1px solid var(--border-dim)' },
  footerItem: { display: 'flex', flexDirection: 'column', alignItems: 'center', minWidth: 54, lineHeight: 1.1 },
};

window.PFD = PFD;
