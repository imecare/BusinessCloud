import React from 'react';
import {
  AbsoluteFill,
  Easing,
  Img,
  Sequence,
  interpolate,
  spring,
  staticFile,
  useCurrentFrame,
  useVideoConfig,
} from 'remotion';

type TutorialProps = {
  appName?: string;
  primaryColor?: string;
  accentColor?: string;
};

type SceneData = {
  image: string;
  eyebrow: string;
  title: string;
  description: string;
  step: number;
  duration: number;
  focus?: {x: number; y: number; width: number; height: number};
  align?: 'top' | 'center' | 'bottom';
};

const scenes: SceneData[] = [
  {
    image: 'screens/dashboard.png',
    eyebrow: 'Todo empieza en un solo lugar',
    title: 'Controla tu bazar de principio a fin',
    description: 'Ventas, cobros, entregas y resultados conectados en un mismo flujo.',
    step: 1,
    duration: 180,
    focus: {x: 20, y: 13, width: 60, height: 28},
  },
  {
    image: 'screens/create-event.png',
    eyebrow: 'Paso 1 · Organiza',
    title: 'Crea un evento en segundos',
    description: 'Define el nombre y la fecha límite de pago para iniciar el ciclo de venta.',
    step: 2,
    duration: 210,
    focus: {x: 33, y: 19, width: 49, height: 42},
  },
  {
    image: 'screens/sales.png',
    eyebrow: 'Paso 2 · Registra',
    title: 'Agrega cada venta al cliente correcto',
    description: 'Selecciona el evento, busca al cliente y captura sus productos con precio.',
    step: 3,
    duration: 210,
    focus: {x: 21, y: 22, width: 76, height: 44},
  },
  {
    image: 'screens/totals-select.png',
    eyebrow: 'Paso 3 · Cierra la venta',
    title: 'Selecciona los eventos a cobrar',
    description: 'Bazar-Enlace calcula automáticamente clientes, saldos y cierres pendientes.',
    step: 4,
    duration: 170,
    focus: {x: 20, y: 36, width: 77, height: 22},
  },
  {
    image: 'screens/totals-preview.png',
    eyebrow: 'Entrega y fechas',
    title: 'Configura fechas generales y por grupo',
    description: 'Ajusta pago, entrega oficial y calendario de cada grupo antes del envío.',
    step: 5,
    duration: 180,
    focus: {x: 20, y: 42, width: 78, height: 47},
  },
  {
    image: 'screens/totals-messages.png',
    eyebrow: 'Mensajes listos',
    title: 'Revisa cada total antes de enviarlo',
    description: 'Cada cliente recibe su importe y un enlace personal para subir el comprobante.',
    step: 6,
    duration: 200,
    focus: {x: 20, y: 24, width: 78, height: 58},
  },
  {
    image: 'screens/whatsapp-sent.png',
    eyebrow: 'WhatsApp conectado',
    title: 'Envía todos los cobros con un clic',
    description: 'La pantalla confirma los mensajes enviados y conserva el enlace de cada cliente.',
    step: 7,
    duration: 180,
    focus: {x: 20, y: 28, width: 78, height: 50},
  },
  {
    image: 'screens/customer-receipt.png',
    eyebrow: 'Experiencia del cliente',
    title: 'El cliente sube su comprobante',
    description: 'Desde su enlace consulta el total, elige el método de pago y adjunta su recibo.',
    step: 8,
    duration: 220,
    focus: {x: 24, y: 19, width: 53, height: 72},
    align: 'bottom',
  },
  {
    image: 'screens/confirm-sale.png',
    eyebrow: 'Validación de pagos',
    title: 'Confirma la venta con evidencia',
    description: 'El equipo revisa referencia, monto y comprobante antes de aprobar el pago.',
    step: 9,
    duration: 200,
    focus: {x: 20, y: 20, width: 77, height: 64},
  },
  {
    image: 'screens/delivery-labels.png',
    eyebrow: 'Logística inteligente',
    title: 'Genera etiquetas y hojas de despacho',
    description: 'Agrupa por recolector, divide paquetes e imprime las etiquetas de entrega.',
    step: 10,
    duration: 220,
    focus: {x: 25, y: 20, width: 67, height: 77},
  },
  {
    image: 'screens/finish-delivery.png',
    eyebrow: 'Cierre simplificado',
    title: 'Un solo comprobante para todos',
    description: 'Adjunta una firma o foto general, aplícala a los grupos y finaliza la entrega.',
    step: 11,
    duration: 220,
    focus: {x: 24, y: 18, width: 69, height: 70},
  },
  {
    image: 'screens/sales-reports.png',
    eyebrow: 'Decisiones con datos',
    title: 'Consulta reportes generales de venta',
    description: 'Detecta rechazos, cancelaciones, retiros pendientes y descarga reportes por evento.',
    step: 12,
    duration: 200,
    focus: {x: 23, y: 17, width: 70, height: 61},
  },
  {
    image: 'screens/event-stages.png',
    eyebrow: 'Visión completa',
    title: 'Revisa cada evento y su etapa',
    description: 'Activo, cerrado, en entrega o finalizado: todo el avance queda visible.',
    step: 13,
    duration: 200,
    focus: {x: 21, y: 40, width: 75, height: 43},
  },
];

const palette = {
  navy: '#081426',
  card: '#101f36',
  white: '#f8fafc',
  muted: '#b6c4d8',
};

const Intro: React.FC<TutorialProps> = ({appName = 'Bazar-Enlace', primaryColor = '#5745f5', accentColor = '#18b7dd'}) => {
  const frame = useCurrentFrame();
  const {fps} = useVideoConfig();
  const enter = spring({frame, fps, config: {damping: 15, stiffness: 90}});
  const line = interpolate(frame, [18, 105], [0, 1], {extrapolateLeft: 'clamp', extrapolateRight: 'clamp'});
  return (
    <AbsoluteFill style={{background: palette.navy, color: palette.white, fontFamily: 'Inter, Arial, sans-serif', overflow: 'hidden'}}>
      <div style={{position: 'absolute', width: 780, height: 780, borderRadius: 999, background: primaryColor, filter: 'blur(170px)', opacity: 0.35, left: -310, top: -260}} />
      <div style={{position: 'absolute', width: 680, height: 680, borderRadius: 999, background: accentColor, filter: 'blur(190px)', opacity: 0.23, right: -270, bottom: -230}} />
      <div style={{padding: '220px 78px 0', transform: `translateY(${(1 - enter) * 90}px)`, opacity: enter}}>
        <div style={{fontSize: 28, letterSpacing: 6, textTransform: 'uppercase', color: '#7dd3fc', fontWeight: 800}}>Recorrido completo</div>
        <h1 style={{fontSize: 106, lineHeight: 0.98, margin: '34px 0 42px', letterSpacing: -5}}>{appName}</h1>
        <p style={{fontSize: 43, lineHeight: 1.3, color: palette.muted, margin: 0}}>De crear un evento<br/>a completar la entrega.</p>
      </div>
      <div style={{position: 'absolute', left: 78, right: 78, bottom: 230}}>
        <div style={{height: 5, borderRadius: 10, background: '#25344c'}}>
          <div style={{width: `${line * 100}%`, height: '100%', borderRadius: 10, background: `linear-gradient(90deg, ${primaryColor}, ${accentColor})`}} />
        </div>
        <div style={{marginTop: 28, fontSize: 28, color: palette.muted}}>Interfaz real · Flujo real · Datos demostrativos</div>
      </div>
    </AbsoluteFill>
  );
};

const BrowserScene: React.FC<{data: SceneData; primaryColor: string; accentColor: string}> = ({data, primaryColor, accentColor}) => {
  const frame = useCurrentFrame();
  const {fps} = useVideoConfig();
  const enter = spring({frame, fps, config: {damping: 18, stiffness: 120}});
  const exit = interpolate(frame, [data.duration - 18, data.duration], [1, 0], {easing: Easing.in(Easing.quad), extrapolateLeft: 'clamp', extrapolateRight: 'clamp'});
  const zoom = interpolate(frame, [0, data.duration], [1, 1.055], {easing: Easing.inOut(Easing.quad), extrapolateRight: 'clamp'});
  const callout = spring({frame: frame - 38, fps, config: {damping: 16, stiffness: 110}});
  const screenshotTop = data.align === 'bottom' ? 455 : data.align === 'top' ? 310 : 370;

  return (
    <AbsoluteFill style={{background: palette.navy, color: palette.white, fontFamily: 'Inter, Arial, sans-serif', opacity: exit, overflow: 'hidden'}}>
      <div style={{position: 'absolute', inset: 0, background: `radial-gradient(circle at 92% 12%, ${primaryColor}33, transparent 32%), radial-gradient(circle at 5% 85%, ${accentColor}22, transparent 36%)`}} />
      <div style={{position: 'absolute', top: 92, left: 64, right: 64, transform: `translateY(${(1 - enter) * -45}px)`, opacity: enter}}>
        <div style={{display: 'flex', alignItems: 'center', gap: 20}}>
          <div style={{height: 64, minWidth: 64, padding: '0 18px', borderRadius: 22, display: 'grid', placeItems: 'center', background: `linear-gradient(135deg, ${primaryColor}, ${accentColor})`, fontSize: 29, fontWeight: 900}}>{String(data.step).padStart(2, '0')}</div>
          <div style={{fontSize: 24, letterSpacing: 3.4, textTransform: 'uppercase', color: '#8be5ff', fontWeight: 800}}>{data.eyebrow}</div>
        </div>
        <h2 style={{fontSize: 61, lineHeight: 1.03, letterSpacing: -2.4, margin: '25px 0 0', maxWidth: 930}}>{data.title}</h2>
      </div>

      <div style={{position: 'absolute', top: screenshotTop, left: 53, width: 974, height: 735, borderRadius: 34, background: '#dae3ee', boxShadow: '0 42px 95px #0009', overflow: 'hidden', border: '2px solid #ffffff28', transform: `translateY(${(1 - enter) * 70}px) scale(${zoom})`, opacity: enter, transformOrigin: 'center'}}>
        <div style={{height: 43, background: '#e8edf4', display: 'flex', alignItems: 'center', paddingLeft: 19, gap: 10, position: 'relative', zIndex: 3}}>
          <span style={{width: 13, height: 13, borderRadius: 20, background: '#ff605c'}} />
          <span style={{width: 13, height: 13, borderRadius: 20, background: '#ffbd44'}} />
          <span style={{width: 13, height: 13, borderRadius: 20, background: '#00ca4e'}} />
          <div style={{height: 22, width: 520, borderRadius: 10, marginLeft: 18, background: '#fff', color: '#8290a4', fontSize: 12, display: 'grid', placeItems: 'center'}}>bazar-enlace.app</div>
        </div>
        <div style={{position: 'relative', width: '100%', height: 692, overflow: 'hidden', background: '#eef3f8'}}>
          <Img src={staticFile(data.image)} style={{width: '100%', height: '100%', objectFit: 'cover'}} />
          {data.focus && (
            <div style={{position: 'absolute', left: `${data.focus.x}%`, top: `${data.focus.y}%`, width: `${data.focus.width}%`, height: `${data.focus.height}%`, border: `5px solid ${accentColor}`, borderRadius: 20, boxShadow: `0 0 0 999px #07142526, 0 0 35px ${accentColor}88`, opacity: callout, transform: `scale(${0.96 + callout * 0.04})`}} />
          )}
        </div>
      </div>

      <div style={{position: 'absolute', left: 62, right: 62, bottom: 105, borderRadius: 34, background: '#10213aee', border: '1px solid #ffffff20', padding: '42px 43px', boxShadow: '0 24px 70px #0008', transform: `translateY(${(1 - enter) * 80}px)`, opacity: enter}}>
        <div style={{fontSize: 34, lineHeight: 1.32, color: palette.muted}}>{data.description}</div>
        <div style={{marginTop: 30, display: 'flex', gap: 10}}>
          {scenes.map((scene) => <span key={scene.step} style={{height: 7, flex: scene.step === data.step ? 3 : 1, borderRadius: 20, background: scene.step <= data.step ? (scene.step === data.step ? accentColor : primaryColor) : '#314158'}} />)}
        </div>
      </div>
    </AbsoluteFill>
  );
};

const Outro: React.FC<TutorialProps> = ({appName = 'Bazar-Enlace', primaryColor = '#5745f5', accentColor = '#18b7dd'}) => {
  const frame = useCurrentFrame();
  const {fps} = useVideoConfig();
  const enter = spring({frame, fps, config: {damping: 14, stiffness: 95}});
  return (
    <AbsoluteFill style={{background: `linear-gradient(145deg, ${palette.navy}, #102241)`, color: palette.white, fontFamily: 'Inter, Arial, sans-serif', display: 'flex', alignItems: 'center', justifyContent: 'center', textAlign: 'center', overflow: 'hidden'}}>
      <div style={{position: 'absolute', width: 850, height: 850, borderRadius: 999, background: `linear-gradient(135deg, ${primaryColor}, ${accentColor})`, filter: 'blur(180px)', opacity: 0.26}} />
      <div style={{width: 900, transform: `scale(${0.84 + enter * 0.16})`, opacity: enter}}>
        <div style={{fontSize: 28, color: '#8be5ff', textTransform: 'uppercase', letterSpacing: 5, fontWeight: 800}}>Más orden. Menos trabajo manual.</div>
        <h2 style={{fontSize: 92, lineHeight: 1.02, letterSpacing: -4, margin: '42px 0'}}>{appName}</h2>
        <p style={{fontSize: 43, lineHeight: 1.3, color: palette.muted, margin: '0 auto 62px'}}>Tu bazar conectado desde la primera venta hasta la última entrega.</p>
        <div style={{display: 'inline-flex', padding: '27px 48px', borderRadius: 25, background: `linear-gradient(135deg, ${primaryColor}, ${accentColor})`, fontSize: 34, fontWeight: 900, boxShadow: `0 24px 60px ${primaryColor}55`}}>Vende · Cobra · Entrega</div>
      </div>
    </AbsoluteFill>
  );
};

export const BazarExtendedTutorial: React.FC<TutorialProps> = (props) => {
  const primaryColor = props.primaryColor ?? '#5745f5';
  const accentColor = props.accentColor ?? '#18b7dd';
  let cursor = 150;
  return (
    <AbsoluteFill>
      <Sequence durationInFrames={150}><Intro {...props} /></Sequence>
      {scenes.map((scene) => {
        const from = cursor;
        cursor += scene.duration;
        return (
          <Sequence key={scene.image} from={from} durationInFrames={scene.duration}>
            <BrowserScene data={scene} primaryColor={primaryColor} accentColor={accentColor} />
          </Sequence>
        );
      })}
      <Sequence from={cursor} durationInFrames={150}><Outro {...props} /></Sequence>
    </AbsoluteFill>
  );
};

export const extendedTutorialDuration = 150 + scenes.reduce((total, scene) => total + scene.duration, 0) + 150;
