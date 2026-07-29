import {chromium} from 'playwright-core';
import {ensureBrowser} from '@remotion/renderer';
import fs from 'node:fs/promises';
import path from 'node:path';

const appUrl = process.env.BAZAR_APP_URL ?? 'http://localhost:4200';
const browserStatus = await ensureBrowser({
  browserExecutable: process.env.BAZAR_BROWSER_PATH ?? null,
  chromeMode: 'headless-shell',
});
if (!('path' in browserStatus)) {
  throw new Error('Remotion could not install or locate a compatible browser.');
}
const browserPath = browserStatus.path;
const outputDir = path.resolve('public/screens');
await fs.mkdir(outputDir, {recursive: true});

const now = new Date();
const iso = (days = 0) => new Date(now.getTime() + days * 86400000).toISOString();
const dashboard = {
  weeklySales: 48920,
  totalSent: 48920,
  totalPaid: 39640,
  totalPending: 9280,
  delinquentsCount: 12,
  messagesAvailable: 1840,
  pendingValidationCount: 7,
  rejectedProofCount: 2,
  customersWithPendingBalance: 18,
  pendingWithdrawalsToValidate: 3,
  closuresInDelivery: 2,
  finalizedClosures: 14,
  recoveryRate: 81,
  collectorVolumes: [
    {collectorId: 1, collectorName: 'Zona Norte', customerCount: 18, totalSales: 14220, totalCollected: 12800},
    {collectorId: 2, collectorName: 'Centro', customerCount: 12, totalSales: 10840, totalCollected: 9210},
    {collectorId: 3, collectorName: 'Zona Sur', customerCount: 9, totalSales: 8410, totalCollected: 7630},
  ],
  delinquents: [
    {customerId: 1, customerName: 'María González', customerPhone: '55 0000 1001', balance: 850, paymentDeadline: iso(-2)},
    {customerId: 2, customerName: 'Laura Pérez', customerPhone: '55 0000 1002', balance: 1250, paymentDeadline: iso(-1)},
  ],
};
const settings = {
  bazarName: 'Bazar Demo',
  logoUrl: null,
  physicalAddress: 'Ciudad de México',
  facebookPageUrl: null,
  primaryColor: '#4f46e5',
  secondaryColor: '#7c3aed',
  labelTagline: 'Ventas, cobros y entregas bajo control',
  salesWhatsApp: null,
  generalWhatsApp: null,
  secondaryWhatsApp: null,
  secondaryWhatsAppDescription: null,
  secondaryWhatsAppShowInProof: false,
  withdrawalWithoutCardEnabled: false,
  withdrawalWithoutCardMessage: null,
  paymentCutoffTime: null,
  phones: [],
  facebookProfiles: [],
};
const events = [
  {
    id: 101,
    description: 'Bazar de verano',
    paymentDeadline: iso(7),
    status: 1,
    statusName: 'Activo',
    totalEventSales: 48920,
    unsentSalesAmount: 9280,
    hasSentSales: false,
    uniqueCustomersCount: 39,
    totalCustomers: 39,
    totalAmount: 48920,
    totalPaid: 39640,
    totalPending: 9280,
    createdAt: iso(-5),
  },
  {
    id: 102,
    description: 'Especial de temporada',
    paymentDeadline: iso(4),
    status: 2,
    statusName: 'Cerrado',
    totalEventSales: 28640,
    unsentSalesAmount: 0,
    hasSentSales: true,
    uniqueCustomersCount: 22,
    totalCustomers: 22,
    totalAmount: 28640,
    totalPaid: 25300,
    totalPending: 3340,
    lastClosureOfficialDeliveryDate: iso(5),
    createdAt: iso(-12),
  },
  {
    id: 103,
    description: 'Accesorios y hogar',
    paymentDeadline: iso(-2),
    status: 3,
    statusName: 'En entrega',
    totalEventSales: 18450,
    unsentSalesAmount: 0,
    hasSentSales: true,
    uniqueCustomersCount: 14,
    totalCustomers: 14,
    totalAmount: 18450,
    totalPaid: 18450,
    totalPending: 0,
    lastClosureOfficialDeliveryDate: iso(1),
    createdAt: iso(-20),
  },
  {
    id: 104,
    description: 'Bazar de primavera',
    paymentDeadline: iso(-18),
    status: 4,
    statusName: 'Finalizado',
    totalEventSales: 37120,
    unsentSalesAmount: 0,
    hasSentSales: true,
    uniqueCustomersCount: 31,
    totalCustomers: 31,
    totalAmount: 37120,
    totalPaid: 37120,
    totalPending: 0,
    lastClosureOfficialDeliveryDate: iso(-12),
    createdAt: iso(-35),
  },
];
const customers = [
  {id: 1, name: 'María González', phone: '55 0000 1001', status: 1, bzaCollectorId: 1, collectorName: 'Zona Norte'},
  {id: 2, name: 'Laura Pérez', phone: '55 0000 1002', status: 1, bzaCollectorId: 2, collectorName: 'Centro'},
  {id: 3, name: 'Ana López', phone: '55 0000 1003', status: 1, bzaCollectorId: 3, collectorName: 'Zona Sur'},
];
const closures = [
  {id: 201, description: 'Cierre · Bazar de verano', officialDeliveryDate: iso(10), paymentDeadline: iso(3), status: 2, inDeliveryProcess: false, delivered: false, createdAt: iso(-2), customerCount: 18, proofsReceived: 14, totalAmount: 22480},
  {id: 202, description: 'Cierre · Especial de temporada', officialDeliveryDate: iso(6), paymentDeadline: iso(1), status: 1, inDeliveryProcess: false, delivered: false, createdAt: iso(-4), customerCount: 12, proofsReceived: 8, totalAmount: 15690},
  {id: 203, description: 'Cierre · Accesorios', officialDeliveryDate: iso(4), paymentDeadline: iso(-1), status: 3, inDeliveryProcess: true, delivered: false, createdAt: iso(-7), customerCount: 9, proofsReceived: 9, totalAmount: 10750},
];
const notificationSettings = {
  chargeMessage: 'Hola {cliente}, tu total es {total}. Sube tu comprobante aquí: {enlace}',
  paymentDueSoonMessage: 'Tu fecha límite de pago está próxima.',
  paymentOverdueMessage: 'Tu pago se encuentra vencido.',
  saleCancelledMessage: 'Tu venta fue cancelada.',
  proofValidatedMessage: 'Tu comprobante fue validado.',
  withdrawalValidatedMessage: 'Tu retiro fue validado.',
  cards: [
    {id: 1, cardNumber: '**** **** **** 7392', cardHolderName: 'Bazar Demo', bank: 'Banco Demo', notes: 'Transferencias', isActive: true},
  ],
};
const totalsPreview = {
  events: [{eventId: 101, description: 'Bazar de verano', pending: 9280, customerCount: 18}],
  groups: [
    {groupId: 1, groupName: 'Zona Norte', deliveryDay: 6, suggestedDeliveryDate: iso(7).slice(0, 10), customerCount: 10, pending: 5420},
    {groupId: 2, groupName: 'Centro', deliveryDay: 0, suggestedDeliveryDate: iso(8).slice(0, 10), customerCount: 8, pending: 3860},
  ],
  customerCount: 18,
  totalAmount: 9280,
  suggestedPaymentDeadline: iso(3).slice(0, 10),
  paymentCutoffTime: '18:00',
};
const totalsResult = {
  closureEventId: 201,
  description: 'Cierre · Bazar de verano',
  messages: [
    {
      customerId: 1,
      customerName: 'María González',
      customerPhone: '5500001001',
      total: 850,
      uploadToken: 'demo-token',
      deliveryDate: iso(7),
      message: 'Hola María, tu total del Bazar de verano es $850.00. Sube tu comprobante aquí: __UPLOAD_LINK__',
    },
    {
      customerId: 2,
      customerName: 'Laura Pérez',
      customerPhone: '5500001002',
      total: 1250,
      uploadToken: 'demo-token-2',
      deliveryDate: iso(8),
      message: 'Hola Laura, tu total del Bazar de verano es $1,250.00. Sube tu comprobante aquí: __UPLOAD_LINK__',
    },
  ],
};
const whatsappResult = {
  closureEventId: 201,
  total: 2,
  sent: 2,
  failed: 0,
  items: [
    {closureCustomerTotalId: 501, customerId: 1, customerName: 'María González', toPhone: '5500001001', sent: true, error: null},
    {closureCustomerTotalId: 502, customerId: 2, customerName: 'Laura Pérez', toPhone: '5500001002', sent: true, error: null},
  ],
};
const customerReceipt = {
  customerName: 'María González',
  totalAmount: 850,
  paymentDeadline: iso(3),
  deliveryDate: iso(7),
  proofUploaded: false,
  proofImageUrl: null,
  proofs: [],
  closureDescription: 'Cierre · Bazar de verano',
  status: 1,
  rejectionReason: null,
  cancellationReason: null,
  customerReference: null,
  withdrawalBank: null,
  paymentMethod: 0,
  bazarName: 'Bazar Demo',
  bazarLogoUrl: null,
  activeCards: [
    {cardNumber: '**** **** **** 7392', cardHolderName: 'Bazar Demo', bank: 'Banco Demo', notes: 'Transferencias'},
  ],
  products: [
    {description: 'Bolso tejido artesanal', price: 850},
  ],
  withdrawalWithoutCardEnabled: true,
  withdrawalWithoutCardMessage: 'Solicita tu código de retiro por WhatsApp.',
  salesWhatsApp: '5500000000',
  secondaryWhatsApp: null,
  secondaryWhatsAppDescription: null,
  facebookPageUrl: null,
  withdrawalWithoutCardEnabled: true,
  paymentCutoffTime: '18:00',
  chargeMessage: null,
  webPushEnabled: false,
  webPushPublicKey: null,
  otherPendingAccounts: [],
  delivered: false,
  deliveredAt: null,
  deliveryProofImageUrl: null,
};
const closureDetail = {
  id: 201,
  description: 'Cierre · Bazar de verano',
  officialDeliveryDate: iso(7),
  paymentDeadline: iso(3),
  status: 2,
  createdAt: iso(-2),
  eventNames: ['Bazar de verano'],
  totalAmount: 22480,
  bazarMessengerUsername: null,
  customers: [
    {
      id: 501,
      customerId: 1,
      customerName: 'María González',
      customerPhone: '5500001001',
      customerFacebookName: 'MariaG',
      groupName: 'Zona Norte',
      totalAmount: 850,
      status: 2,
      proofImageUrl: `${appUrl}/bazar-hub-logo.png`,
      proofUploadedAt: iso(-1),
      uploadToken: 'demo-token',
      rejectionReason: null,
      customerJustification: null,
      resubmitted: false,
      proofs: [{id: 1, url: `${appUrl}/bazar-hub-logo.png`, uploadedAt: iso(-1)}],
      cancellationReason: null,
      cancelledIsCustomerFault: null,
      paymentMethod: 1,
      customerReference: 'BAZ-73921',
      proofUploadedByBazar: false,
      validatedWithoutProof: false,
      validationNote: null,
      message: 'Hola María, tu total es $850.00.',
    },
    {
      id: 502,
      customerId: 2,
      customerName: 'Laura Pérez',
      customerPhone: '5500001002',
      customerFacebookName: null,
      groupName: 'Centro',
      totalAmount: 1250,
      status: 3,
      proofImageUrl: `${appUrl}/bazar-hub-logo.png`,
      proofUploadedAt: iso(-1),
      uploadToken: 'demo-token-2',
      rejectionReason: null,
      customerJustification: null,
      resubmitted: false,
      proofs: [{id: 2, url: `${appUrl}/bazar-hub-logo.png`, uploadedAt: iso(-1)}],
      cancellationReason: null,
      cancelledIsCustomerFault: null,
      paymentMethod: 1,
      customerReference: 'BAZ-73922',
      proofUploadedByBazar: false,
      validatedWithoutProof: false,
      validationNote: null,
      message: 'Hola Laura, tu comprobante fue validado.',
    },
  ],
};
const deliveryLabels = {
  closureEventId: 201,
  eventDescription: 'Cierre · Bazar de verano',
  officialDeliveryDate: iso(7),
  inDeliveryProcess: false,
  bazar: {bazarName: 'Bazar Demo', logoUrl: null},
  groups: [
    {groupName: 'Zona Norte', customerCount: 2},
    {groupName: 'Centro', customerCount: 1},
  ],
  customers: [
    {customerId: 1, customerName: 'María González', groupName: 'Zona Norte', collectorName: 'Carlos', productCount: 1, totalAmount: 850, status: 3, products: [{id: 1, description: 'Bolso tejido artesanal'}]},
    {customerId: 2, customerName: 'Laura Pérez', groupName: 'Centro', collectorName: 'Ana', productCount: 2, totalAmount: 1250, status: 3, products: [{id: 2, description: 'Set de cocina'}, {id: 3, description: 'Organizador'}]},
  ],
};
const deliveryProofs = {
  closureEventId: 203,
  description: 'Cierre · Accesorios',
  inDeliveryProcess: true,
  delivered: false,
  deliveredAt: null,
  groups: [
    {collectorGroupId: 1, groupName: 'Zona Norte', customerCount: 5},
    {collectorGroupId: 2, groupName: 'Centro', customerCount: 4},
  ],
  proofs: [
    {id: 801, collectorGroupId: null, groupName: 'General · todos los grupos', imageUrl: `${appUrl}/bazar-hub-logo.png`, uploadedAt: iso(0)},
  ],
};
const rejectedReport = {
  rejections: [
    {id: 1, customerId: 4, customerName: 'Sofía Ramírez', customerPhone: '55 0000 1004', eventDescription: 'Especial de temporada', totalAmount: 920, reason: 'Imagen ilegible', reference: 'REF-1001', proofUrls: [], rejectedAt: iso(-4)},
    {id: 2, customerId: 4, customerName: 'Sofía Ramírez', customerPhone: '55 0000 1004', eventDescription: 'Bazar de verano', totalAmount: 640, reason: 'Monto incorrecto', reference: 'REF-1002', proofUrls: [], rejectedAt: iso(-1)},
    {id: 3, customerId: 5, customerName: 'Patricia Díaz', customerPhone: '55 0000 1005', eventDescription: 'Bazar de verano', totalAmount: 1180, reason: 'Referencia no encontrada', reference: 'REF-1003', proofUrls: [], rejectedAt: iso(-2)},
  ],
  repeatOffenders: [
    {customerId: 4, customerName: 'Sofía Ramírez', customerPhone: '55 0000 1004', rejectionCount: 2, lastRejectedAt: iso(-1)},
  ],
  totalCustomers: 2,
  totalRejections: 3,
};

const json = (route, value, status = 200) =>
  route.fulfill({status, contentType: 'application/json', body: JSON.stringify(value)});

const browser = await chromium.launch({
  executablePath: browserPath,
  headless: true,
  args: ['--ignore-certificate-errors'],
});
const context = await browser.newContext({
  viewport: {width: 1440, height: 1000},
  deviceScaleFactor: 1,
  ignoreHTTPSErrors: true,
});

const header = Buffer.from(JSON.stringify({alg: 'none', typ: 'JWT'})).toString('base64url');
const payload = Buffer.from(JSON.stringify({
  exp: Math.floor(Date.now() / 1000) + 3600,
  tenant_id: 'demo',
  role: 'SuperAdmin',
})).toString('base64url');
await context.addInitScript(({token}) => {
  localStorage.setItem('authToken', token);
  localStorage.setItem('tenantId', 'demo');
  localStorage.setItem('userRole', 'SuperAdmin');
  localStorage.setItem('userName', 'Administradora Demo');
  localStorage.setItem('userEmail', 'demo@bazar.local');
  localStorage.setItem('userId', 'demo-user');
  localStorage.setItem('sellerId', '');
  localStorage.setItem('mustChangePassword', 'false');
  localStorage.setItem('canViewTotals', 'true');
  localStorage.setItem('allowedModules', JSON.stringify(['Bazares']));
}, {token: `${header}.${payload}.demo`});

await context.route('**/api/**', async (route) => {
  const url = new URL(route.request().url());
  const pathname = url.pathname.toLowerCase();
  if (pathname.endsWith('/api/bazares/bzadashboard')) return json(route, dashboard);
  if (pathname.endsWith('/api/bazares/bzabazarsettings')) return json(route, settings);
  if (pathname.endsWith('/api/bazares/bzaevents') && route.request().method() === 'GET') return json(route, events);
  if (pathname.endsWith('/api/bazares/bzaevents') && route.request().method() === 'POST') return json(route, {...events[0], id: 105, description: 'Nuevo evento de demostración'});
  if (pathname.endsWith('/api/bazares/bzaevents/101')) return json(route, {
    ...events[0],
    metrics: {totalRevenue: 48920, productsCount: 64, uniqueCustomersCount: 39, uniqueCustomers: 39, totalProducts: 64, totalSales: 48920, totalPaid: 39640, pendingAmount: 9280, totalCollected: 39640, totalPending: 9280, collectionPercentage: 81},
    auditHistory: [{event: 'CREATED', timestamp: iso(-5), userName: 'Administradora Demo', details: 'Evento creado'}],
  });
  if (pathname.endsWith('/api/bazares/bzacustomers')) return json(route, customers);
  if (pathname.endsWith('/api/bazares/bzanotificationsettings')) return json(route, notificationSettings);
  if (pathname.endsWith('/api/bazares/bzatotals/preview')) return json(route, totalsPreview);
  if (pathname.endsWith('/api/bazares/bzatotals/send')) return json(route, totalsResult);
  if (pathname.endsWith('/api/bazares/bzatotals/201/send-whatsapp')) return json(route, whatsappResult);
  if (pathname.endsWith('/api/bazares/bzatotals/201/delivery-labels')) return json(route, deliveryLabels);
  if (pathname.endsWith('/api/bazares/bzatotals/203/delivery-proofs')) return json(route, deliveryProofs);
  if (pathname.endsWith('/api/bazares/bzatotals/201')) return json(route, closureDetail);
  if (pathname.endsWith('/api/bazares/bzatotals')) return json(route, closures);
  if (pathname.endsWith('/api/bazares/bzacomprobantes/demo-token')) return json(route, customerReceipt);
  if (pathname.includes('/api/bazares/bzareports/rejected')) return json(route, rejectedReport);
  if (pathname.includes('/api/bazares/bzatotals/reports/rejected')) return json(route, rejectedReport);
  if (pathname.includes('/bzasoldproducts')) return json(route, {bzaSaleId: 101, eventDescription: 'Bazar de verano', items: []});
  if (pathname.includes('/notification')) return json(route, {});
  return json(route, []);
});

const page = await context.newPage();
page.on('console', (message) => {
  if (message.type() === 'error') console.error(message.text());
});

const capture = async (route, filename) => {
  await page.goto(`${appUrl}${route}`, {waitUntil: 'networkidle'});
  await page.waitForTimeout(700);
  await page.screenshot({path: path.join(outputDir, filename), animations: 'disabled'});
};

await capture('/', 'dashboard.png');

await page.goto(`${appUrl}/products`, {waitUntil: 'networkidle'});
await page.locator('select').first().selectOption({index: 1});
await page.getByPlaceholder('Buscar por nombre o teléfono…').click();
await page.getByText('María González', {exact: true}).last().click();
await page.getByPlaceholder('Ej: Blusa floral talla M').fill('Bolso tejido artesanal');
await page.getByPlaceholder('0.00').first().fill('850');
await page.waitForTimeout(500);
await page.screenshot({path: path.join(outputDir, 'sales.png'), animations: 'disabled'});

await capture('/comprobantes', 'proofs.png');
await capture('/logistics', 'logistics.png');

await capture('/bazares/events', 'event-stages.png');
const newEventButton = page.getByRole('button', {name: /Nuevo Evento|Nuevo/i}).first();
if (await newEventButton.isVisible()) {
  await newEventButton.click();
  await page.waitForTimeout(300);
  await page.screenshot({path: path.join(outputDir, 'create-event.png'), animations: 'disabled'});
}

await page.goto(`${appUrl}/totales`, {waitUntil: 'networkidle'});
await page.waitForTimeout(500);
await page.screenshot({path: path.join(outputDir, 'totals-select.png'), animations: 'disabled'});
await page.locator('.event-row input[type="checkbox"]').first().check();
await page.getByRole('button', {name: 'Continuar'}).click();
await page.waitForTimeout(500);
await page.locator('input[type="date"]').first().fill(iso(7).slice(0, 10));
await page.screenshot({path: path.join(outputDir, 'totals-preview.png'), animations: 'disabled'});
await page.getByRole('button', {name: 'Generar mensajes de envío'}).click();
await page.waitForTimeout(500);
await page.screenshot({path: path.join(outputDir, 'totals-messages.png'), animations: 'disabled'});
await page.getByRole('button', {name: 'Confirmar y enviar por WhatsApp'}).first().click();
await page.waitForTimeout(500);
await page.screenshot({path: path.join(outputDir, 'whatsapp-sent.png'), animations: 'disabled'});

await page.goto(`${appUrl}/comprobante/demo-token`, {waitUntil: 'networkidle'});
await page.getByText('Transferencia', {exact: true}).click();
await page.locator('input[type="file"]').setInputFiles(path.resolve('public/demo-receipt.svg'));
const reference = page.locator('textarea').first();
if (await reference.isVisible()) await reference.fill('Referencia BAZ-73921');
await page.waitForTimeout(400);
await page.screenshot({path: path.join(outputDir, 'customer-receipt.png'), animations: 'disabled'});

await capture('/comprobantes/201', 'confirm-sale.png');

await page.goto(`${appUrl}/logistics`, {waitUntil: 'networkidle'});
await page.getByText('Cierre · Bazar de verano', {exact: true}).click();
await page.waitForTimeout(500);
await page.screenshot({path: path.join(outputDir, 'delivery-labels.png'), animations: 'disabled'});

await page.goto(`${appUrl}/deliveries`, {waitUntil: 'networkidle'});
await page.getByText('Cierre · Accesorios', {exact: true}).click();
await page.waitForTimeout(500);
await page.screenshot({path: path.join(outputDir, 'finish-delivery.png'), animations: 'disabled'});

await capture('/reports', 'sales-reports.png');

await browser.close();
