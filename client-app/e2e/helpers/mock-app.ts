import { Page, Route } from '@playwright/test';

export const AUTH_STORAGE_KEY = 'auction.auth.session';

export type FrontendSession = {
  userId: string;
  email: string;
  fullName: string;
  role: 'Admin' | 'Seller' | 'Bidder';
  avatarUrl: string | null;
  accessToken: string;
  expiresAtUtc: string;
};

export type MockAppOptions = {
  session?: FrontendSession | null;
  watchlistIds?: string[];
};

type ActiveAuction = {
  id: string;
  sellerId: string;
  title: string;
  category: string;
  description: string | null;
  primaryImageId: string | null;
  priceAmount: number;
  currency: string;
  endTimeUtc: string;
  bidCount: number;
};

type MyBidItem = {
  auctionId: string;
  title: string;
  category: string;
  myMaxBidAmount: number;
  currentHighestBidAmount: number;
  currency: string;
  bidCount: number;
  endTimeUtc: string;
  primaryImageId: string | null;
};

type UserProfile = {
  userId: string;
  email: string;
  fullName: string;
  phoneNumber: string | null;
  role: string;
  createdAtUtc: string;
};

type AdminUser = {
  userId: string;
  email: string;
  fullName: string;
  phoneNumber: string | null;
  role: 'Admin' | 'Seller' | 'Bidder';
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
};

type AdminAuctionListItem = {
  auctionId: string;
  title: string;
  sellerId: string;
  sellerName: string;
  category: string;
  currentBidAmount: number;
  currency: string;
  bidCount: number;
  endTimeUtc: string;
  status: 'Draft' | 'Active' | 'Ended';
};

type AdminAuctionDetail = {
  auctionId: string;
  title: string;
  sellerId: string;
  sellerName: string;
  category: string;
  description: string | null;
  startingPriceAmount: number;
  currency: string;
  currentBidAmount: number;
  bidCount: number;
  startTimeUtc: string | null;
  endTimeUtc: string;
  endedAtUtc: string | null;
  status: 'Draft' | 'Active' | 'Ended';
  highestBidderName: string | null;
  primaryImageId: string | null;
  imageCount: number;
  bids: Array<{
    bidId: string;
    bidderId: string;
    bidderName: string;
    amount: number;
    currency: string;
    placedAtUtc: string;
  }>;
};

type AdminTransactionListItem = {
  transactionId: string;
  userId: string;
  userName: string;
  type: string;
  amount: number;
  currency: string;
  status: 'Completed' | 'Refunded';
  createdAtUtc: string;
  updatedAtUtc: string;
};

type AdminTransactionDetail = {
  transactionId: string;
  userId: string;
  userName: string;
  type: string;
  amount: number;
  currency: string;
  status: 'Completed' | 'Refunded';
  reference: string | null;
  description: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
  refundedAtUtc: string | null;
  refundedBy: string | null;
  refundReason: string | null;
  walletBalanceAmount: number | null;
  walletBalanceCurrency: string | null;
};

type AdminFlaggedCase = {
  caseId: string;
  auctionId: string;
  auctionTitle: string;
  reportedByUserId: string;
  reporterName: string;
  reason: string;
  details: string | null;
  status: 'Open' | 'Resolved';
  createdAtUtc: string;
  updatedAtUtc: string;
  resolvedAtUtc: string | null;
  resolvedBy: string | null;
  resolutionNote: string | null;
};

type AdminDashboard = {
  generatedAtUtc: string;
  activeUsers: number;
  liveAuctions: number;
  dailyBids: number;
  flaggedCases: number;
  recentActivity: Array<{
    kind: string;
    title: string;
    description: string;
    occurredAtUtc: string;
  }>;
};

export type MockAppState = {
  activeAuctions: ActiveAuction[];
  myBids: MyBidItem[];
  profile: UserProfile;
  adminUsers: AdminUser[];
  adminAuctionList: AdminAuctionListItem[];
  adminAuctionDetails: Record<string, AdminAuctionDetail>;
  adminTransactions: AdminTransactionListItem[];
  adminTransactionDetails: Record<string, AdminTransactionDetail>;
  adminFlaggedCases: AdminFlaggedCase[];
  adminDashboard: AdminDashboard;
  settingValues: Record<string, string>;
  generatedReportCount: number;
};

export const ids = {
  bidder: 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',
  seller: 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb',
  admin: 'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
  managedUser: 'dddddddd-dddd-4ddd-8ddd-dddddddddddd',
  cameraAuction: '11111111-1111-4111-8111-111111111111',
  consoleAuction: '22222222-2222-4222-8222-222222222222',
  managedTransaction: '33333333-3333-4333-8333-333333333333',
  flaggedCase: '44444444-4444-4444-8444-444444444444',
  bid: '55555555-5555-4555-8555-555555555555'
} as const;

const transparentSvg = '<svg xmlns="http://www.w3.org/2000/svg" width="1" height="1"></svg>';

function createSession(
  userId: string,
  email: string,
  fullName: string,
  role: FrontendSession['role']
): FrontendSession {
  return {
    userId,
    email,
    fullName,
    role,
    avatarUrl: null,
    accessToken: `token-${role.toLowerCase()}`,
    expiresAtUtc: '2027-05-15T12:00:00Z'
  };
}

export const bidderSession = createSession(ids.bidder, 'bidder@example.com', 'Brianna Bidder', 'Bidder');
export const sellerSession = createSession(ids.seller, 'seller@example.com', 'Sam Seller', 'Seller');
export const adminSession = createSession(ids.admin, 'admin@example.com', 'Ada Admin', 'Admin');

export function createMockState(): MockAppState {
  const state: MockAppState = {
    activeAuctions: [
      {
        id: ids.cameraAuction,
        sellerId: ids.seller,
        title: 'Vintage Camera Lot',
        category: 'Collectibles',
        description: 'Limited edition camera with accessories.',
        primaryImageId: null,
        priceAmount: 150,
        currency: 'USD',
        endTimeUtc: '2027-06-01T12:00:00Z',
        bidCount: 3
      },
      {
        id: ids.consoleAuction,
        sellerId: ids.seller,
        title: 'Retro Game Console',
        category: 'Gaming',
        description: 'Boxed console with two controllers.',
        primaryImageId: null,
        priceAmount: 90,
        currency: 'USD',
        endTimeUtc: '2027-06-10T16:30:00Z',
        bidCount: 0
      }
    ],
    myBids: [
      {
        auctionId: ids.cameraAuction,
        title: 'Vintage Camera Lot',
        category: 'Collectibles',
        myMaxBidAmount: 175,
        currentHighestBidAmount: 150,
        currency: 'USD',
        bidCount: 3,
        endTimeUtc: '2027-06-01T12:00:00Z',
        primaryImageId: null
      }
    ],
    profile: {
      userId: ids.bidder,
      email: bidderSession.email,
      fullName: bidderSession.fullName,
      phoneNumber: '+1 (555) 123-4567',
      role: bidderSession.role,
      createdAtUtc: '2026-01-12T00:00:00Z'
    },
    adminUsers: [
      {
        userId: ids.managedUser,
        email: 'managed.user@example.com',
        fullName: 'Managed User',
        phoneNumber: '+1 555 000 1111',
        role: 'Bidder',
        isActive: true,
        createdAtUtc: '2026-02-01T10:00:00Z',
        updatedAtUtc: '2026-05-01T10:00:00Z'
      },
      {
        userId: ids.seller,
        email: sellerSession.email,
        fullName: sellerSession.fullName,
        phoneNumber: '+1 555 222 3333',
        role: 'Seller',
        isActive: true,
        createdAtUtc: '2026-01-20T10:00:00Z',
        updatedAtUtc: '2026-05-02T10:00:00Z'
      }
    ],
    adminAuctionList: [
      {
        auctionId: ids.cameraAuction,
        title: 'Vintage Camera Lot',
        sellerId: ids.seller,
        sellerName: 'Sam Seller',
        category: 'Collectibles',
        currentBidAmount: 150,
        currency: 'USD',
        bidCount: 3,
        endTimeUtc: '2027-06-01T12:00:00Z',
        status: 'Active'
      },
      {
        auctionId: ids.consoleAuction,
        title: 'Retro Game Console',
        sellerId: ids.seller,
        sellerName: 'Sam Seller',
        category: 'Gaming',
        currentBidAmount: 90,
        currency: 'USD',
        bidCount: 0,
        endTimeUtc: '2027-06-10T16:30:00Z',
        status: 'Draft'
      }
    ],
    adminAuctionDetails: {
      [ids.cameraAuction]: {
        auctionId: ids.cameraAuction,
        title: 'Vintage Camera Lot',
        sellerId: ids.seller,
        sellerName: 'Sam Seller',
        category: 'Collectibles',
        description: 'Limited edition camera with accessories.',
        startingPriceAmount: 100,
        currency: 'USD',
        currentBidAmount: 150,
        bidCount: 3,
        startTimeUtc: '2027-05-01T08:00:00Z',
        endTimeUtc: '2027-06-01T12:00:00Z',
        endedAtUtc: null,
        status: 'Active',
        highestBidderName: 'Brianna Bidder',
        primaryImageId: null,
        imageCount: 0,
        bids: [
          {
            bidId: ids.bid,
            bidderId: ids.bidder,
            bidderName: 'Brianna Bidder',
            amount: 150,
            currency: 'USD',
            placedAtUtc: '2027-05-15T09:30:00Z'
          }
        ]
      }
    },
    adminTransactions: [
      {
        transactionId: ids.managedTransaction,
        userId: ids.bidder,
        userName: 'Brianna Bidder',
        type: 'Bid Hold',
        amount: 150,
        currency: 'USD',
        status: 'Completed',
        createdAtUtc: '2027-05-15T09:30:00Z',
        updatedAtUtc: '2027-05-15T09:30:00Z'
      }
    ],
    adminTransactionDetails: {
      [ids.managedTransaction]: {
        transactionId: ids.managedTransaction,
        userId: ids.bidder,
        userName: 'Brianna Bidder',
        type: 'Bid Hold',
        amount: 150,
        currency: 'USD',
        status: 'Completed',
        reference: 'BID-2027-0515',
        description: 'Hold for camera auction',
        createdAtUtc: '2027-05-15T09:30:00Z',
        updatedAtUtc: '2027-05-15T09:30:00Z',
        refundedAtUtc: null,
        refundedBy: null,
        refundReason: null,
        walletBalanceAmount: 450,
        walletBalanceCurrency: 'USD'
      }
    },
    adminFlaggedCases: [
      {
        caseId: ids.flaggedCase,
        auctionId: ids.cameraAuction,
        auctionTitle: 'Vintage Camera Lot',
        reportedByUserId: ids.bidder,
        reporterName: 'Brianna Bidder',
        reason: 'Counterfeit concern',
        details: 'The serial number does not match the certificate image.',
        status: 'Open',
        createdAtUtc: '2027-05-15T10:00:00Z',
        updatedAtUtc: '2027-05-15T10:00:00Z',
        resolvedAtUtc: null,
        resolvedBy: null,
        resolutionNote: null
      },
      {
        caseId: '66666666-6666-4666-8666-666666666666',
        auctionId: ids.consoleAuction,
        auctionTitle: 'Retro Game Console',
        reportedByUserId: ids.bidder,
        reporterName: 'Brianna Bidder',
        reason: 'Duplicate listing',
        details: 'Already reviewed and resolved.',
        status: 'Resolved',
        createdAtUtc: '2027-05-10T10:00:00Z',
        updatedAtUtc: '2027-05-10T11:00:00Z',
        resolvedAtUtc: '2027-05-10T11:00:00Z',
        resolvedBy: 'Ada Admin',
        resolutionNote: 'Merged with primary listing.'
      }
    ],
    adminDashboard: {
      generatedAtUtc: '2027-05-15T12:00:00Z',
      activeUsers: 124,
      liveAuctions: 18,
      dailyBids: 43,
      flaggedCases: 2,
      recentActivity: [
        {
          kind: 'Bid',
          title: 'High bid placed on Vintage Camera Lot',
          description: 'Brianna Bidder placed a new top bid.',
          occurredAtUtc: '2027-05-15T09:30:00Z'
        },
        {
          kind: 'Moderation',
          title: 'Flagged case requires review',
          description: 'A new moderation case entered the queue.',
          occurredAtUtc: '2027-05-15T10:00:00Z'
        }
      ]
    },
    settingValues: {
      maintenanceMode: 'false',
      allowGuestBidding: 'false',
      minBidIncrement: '1',
      auctionExtensionMinutes: '5',
      settlementWindowHours: '48'
    },
    generatedReportCount: 0
  };

  return structuredClone(state);
}

export async function setupMockApp(page: Page, state: MockAppState, options: MockAppOptions = {}): Promise<void> {
  await page.addInitScript(
    ({ authStorageKey, session, watchlistIds }) => {
      (window as Window & { __AUCTION_E2E_DISABLE_SIGNALR__?: boolean }).__AUCTION_E2E_DISABLE_SIGNALR__ = true;

      if (session) {
        localStorage.setItem(authStorageKey, JSON.stringify(session));
      } else {
        localStorage.removeItem(authStorageKey);
      }

      if (session) {
        localStorage.setItem(`auction.watchlist.${session.userId}`, JSON.stringify(watchlistIds ?? []));
      }
    },
    {
      authStorageKey: AUTH_STORAGE_KEY,
      session: options.session ?? null,
      watchlistIds: options.watchlistIds ?? []
    }
  );

  await page.route('https://flagcdn.com/**', async (route) => {
    await route.fulfill({ status: 200, contentType: 'image/svg+xml', body: transparentSvg });
  });

  await page.route('**/hubs/auctions/**', async (route) => {
    await route.fulfill({ status: 204, body: '' });
  });

  await page.route('**/api/**', async (route) => {
    await handleApiRoute(route, state);
  });
}

async function handleApiRoute(route: Route, state: MockAppState): Promise<void> {
  const request = route.request();
  const url = new URL(request.url());
  const path = url.pathname;
  const method = request.method();

  if (method === 'POST' && path === '/api/auth/login') {
    const payload = readJsonBody<{ email?: string }>(request) ?? {};
    const session = payload.email?.includes('admin')
      ? adminSession
      : payload.email?.includes('seller')
        ? sellerSession
        : bidderSession;
    await json(route, 200, session);
    return;
  }

  if (method === 'POST' && path === '/api/auth/register') {
    const payload = readJsonBody<{ email?: string; fullName?: string }>(request) ?? {};
    await json(route, 200, {
      ...bidderSession,
      email: payload.email ?? bidderSession.email,
      fullName: payload.fullName ?? bidderSession.fullName
    });
    return;
  }

  if (method === 'GET' && path === '/api/auctions') {
    await json(route, 200, state.activeAuctions);
    return;
  }

  if (method === 'GET' && path === '/api/auctions/my-bids') {
    await json(route, 200, state.myBids);
    return;
  }

  const bidMatch = /^\/api\/auctions\/([^/]+)\/bids$/.exec(path);
  if (method === 'POST' && bidMatch) {
    const auctionId = bidMatch[1];
    const payload = readJsonBody<{ amount?: number; currency?: string }>(request) ?? {};
    const auction = state.activeAuctions.find((item) => item.id === auctionId);

    if (!auction || typeof payload.amount !== 'number') {
      await json(route, 404, { details: 'Auction was not found.' });
      return;
    }

    auction.priceAmount = payload.amount;
    auction.bidCount += 1;

    const myBid = state.myBids.find((item) => item.auctionId === auctionId);
    if (myBid) {
      myBid.currentHighestBidAmount = payload.amount;
      myBid.myMaxBidAmount = Math.max(myBid.myMaxBidAmount, payload.amount);
      myBid.bidCount += 1;
    }

    await json(route, 200, {
      bidId: ids.bid,
      auctionId,
      bidderId: bidderSession.userId,
      amount: payload.amount,
      currency: payload.currency ?? auction.currency,
      placedAtUtc: '2027-05-15T12:00:00Z',
      currentPriceAmount: payload.amount,
      currentPriceCurrency: payload.currency ?? auction.currency
    });
    return;
  }

  const reportMatch = /^\/api\/auctions\/([^/]+)\/reports$/.exec(path);
  if (method === 'POST' && reportMatch) {
    await json(route, 200, ids.flaggedCase);
    return;
  }

  if (method === 'POST' && path === '/api/auctions') {
    await json(route, 200, '77777777-7777-4777-8777-777777777777');
    return;
  }

  if (method === 'GET' && path === '/api/users/profile') {
    await json(route, 200, state.profile);
    return;
  }

  if (method === 'PUT' && path === '/api/users/profile') {
    const payload = readJsonBody<{ email?: string; fullName?: string; phoneNumber?: string | null }>(request) ?? {};
    state.profile = {
      ...state.profile,
      email: payload.email ?? state.profile.email,
      fullName: payload.fullName ?? state.profile.fullName,
      phoneNumber: payload.phoneNumber ?? state.profile.phoneNumber
    };
    await json(route, 200, state.profile);
    return;
  }

  if (method === 'POST' && path === '/api/users/security/change-password') {
    await route.fulfill({ status: 200, body: '' });
    return;
  }

  if (method === 'GET' && path === '/api/admin/users') {
    await json(route, 200, state.adminUsers);
    return;
  }

  const adminUserMatch = /^\/api\/admin\/users\/([^/]+)$/.exec(path);
  if (adminUserMatch && method === 'PUT') {
    const userId = adminUserMatch[1];
    const payload = readJsonBody<Partial<AdminUser>>(request) ?? {};
    const index = state.adminUsers.findIndex((item) => item.userId === userId);
    if (index === -1) {
      await json(route, 404, { details: 'User was not found.' });
      return;
    }

    state.adminUsers[index] = {
      ...state.adminUsers[index],
      email: typeof payload.email === 'string' ? payload.email : state.adminUsers[index].email,
      fullName: typeof payload.fullName === 'string' ? payload.fullName : state.adminUsers[index].fullName,
      role: payload.role ?? state.adminUsers[index].role,
      isActive: typeof payload.isActive === 'boolean' ? payload.isActive : state.adminUsers[index].isActive,
      phoneNumber: payload.phoneNumber ?? state.adminUsers[index].phoneNumber,
      updatedAtUtc: '2027-05-15T12:05:00Z'
    };

    await json(route, 200, state.adminUsers[index]);
    return;
  }

  if (adminUserMatch && method === 'DELETE') {
    state.adminUsers = state.adminUsers.filter((item) => item.userId !== adminUserMatch[1]);
    await route.fulfill({ status: 204, body: '' });
    return;
  }

  if (method === 'GET' && path === '/api/admin/auctions') {
    await json(route, 200, state.adminAuctionList);
    return;
  }

  const adminAuctionMatch = /^\/api\/admin\/auctions\/([^/]+)$/.exec(path);
  if (adminAuctionMatch && method === 'GET') {
    const detail = state.adminAuctionDetails[adminAuctionMatch[1]];
    if (!detail) {
      await json(route, 404, { details: 'Auction was not found.' });
      return;
    }

    await json(route, 200, detail);
    return;
  }

  const adminAuctionActionMatch = /^\/api\/admin\/auctions\/([^/]+)\/(start|end)$/.exec(path);
  if (adminAuctionActionMatch && method === 'POST') {
    const [, auctionId, action] = adminAuctionActionMatch;
    const listItem = state.adminAuctionList.find((item) => item.auctionId === auctionId);
    const detail = state.adminAuctionDetails[auctionId];

    if (listItem) {
      listItem.status = action === 'start' ? 'Active' : 'Ended';
    }

    if (detail) {
      detail.status = action === 'start' ? 'Active' : 'Ended';
      detail.endedAtUtc = action === 'end' ? '2027-05-15T12:10:00Z' : null;
      detail.startTimeUtc = action === 'start' ? '2027-05-15T12:00:00Z' : detail.startTimeUtc;
    }

    await route.fulfill({ status: 200, body: '' });
    return;
  }

  if (adminAuctionMatch && method === 'PUT') {
    await route.fulfill({ status: 200, body: '' });
    return;
  }

  if (adminAuctionMatch && method === 'DELETE') {
    const auctionId = adminAuctionMatch[1];
    state.adminAuctionList = state.adminAuctionList.filter((item) => item.auctionId !== auctionId);
    delete state.adminAuctionDetails[auctionId];
    await route.fulfill({ status: 204, body: '' });
    return;
  }

  if (method === 'GET' && path === '/api/admin/transactions') {
    await json(route, 200, state.adminTransactions);
    return;
  }

  const adminTransactionMatch = /^\/api\/admin\/transactions\/([^/]+)$/.exec(path);
  if (adminTransactionMatch && method === 'GET') {
    const detail = state.adminTransactionDetails[adminTransactionMatch[1]];
    if (!detail) {
      await json(route, 404, { details: 'Transaction was not found.' });
      return;
    }

    await json(route, 200, detail);
    return;
  }

  const refundMatch = /^\/api\/admin\/transactions\/([^/]+)\/refund$/.exec(path);
  if (refundMatch && method === 'POST') {
    const transactionId = refundMatch[1];
    const payload = readJsonBody<{ reason?: string }>(request) ?? {};
    const detail = state.adminTransactionDetails[transactionId];
    const listItem = state.adminTransactions.find((item) => item.transactionId === transactionId);

    if (!detail || !listItem) {
      await json(route, 404, { details: 'Transaction was not found.' });
      return;
    }

    detail.status = 'Refunded';
    detail.refundReason = payload.reason ?? 'Refunded by admin';
    detail.refundedAtUtc = '2027-05-15T12:15:00Z';
    detail.refundedBy = adminSession.fullName;
    listItem.status = 'Refunded';

    await json(route, 200, detail);
    return;
  }

  if (method === 'GET' && path === '/api/admin/reports/dashboard') {
    await json(route, 200, state.adminDashboard);
    return;
  }

  if (method === 'POST' && path === '/api/admin/reports/generate') {
    state.generatedReportCount += 1;
    await json(route, 200, {
      reportType: 'performance',
      rangeStartUtc: '2027-05-01T00:00:00Z',
      rangeEndUtc: '2027-05-15T00:00:00Z',
      generatedAtUtc: `2027-05-15T12:${String(state.generatedReportCount).padStart(2, '0')}:00Z`,
      metrics: {
        averageBidAmount: 245,
        conversionRate: 68,
        liveAuctions: 18,
        resolvedCases: 12
      },
      totals: {
        totalRevenue: 22500,
        totalBids: 143,
        totalAuctions: 18,
        activeAuctions: 12
      }
    });
    return;
  }

  if (method === 'GET' && path === '/api/admin/moderation/cases') {
    const includeResolved = url.searchParams.get('includeResolved') === 'true';
    const items = includeResolved
      ? state.adminFlaggedCases
      : state.adminFlaggedCases.filter((item) => item.status === 'Open');
    await json(route, 200, items);
    return;
  }

  const flaggedCaseMatch = /^\/api\/admin\/moderation\/cases\/([^/]+)\/resolve$/.exec(path);
  if (flaggedCaseMatch && method === 'POST') {
    const caseId = flaggedCaseMatch[1];
    const payload = readJsonBody<{ resolutionNote?: string }>(request) ?? {};
    const item = state.adminFlaggedCases.find((entry) => entry.caseId === caseId);
    if (!item) {
      await json(route, 404, { details: 'Case was not found.' });
      return;
    }

    item.status = 'Resolved';
    item.resolutionNote = payload.resolutionNote ?? 'Resolved in test';
    item.resolvedAtUtc = '2027-05-15T12:20:00Z';
    item.resolvedBy = adminSession.fullName;
    item.updatedAtUtc = '2027-05-15T12:20:00Z';

    await json(route, 200, item);
    return;
  }

  const settingsMatch = /^\/api\/admin\/settings\/([^/]+)$/.exec(path);
  if (settingsMatch && method === 'PUT') {
    const key = decodeURIComponent(settingsMatch[1]);
    const payload = readJsonBody<{ value?: string }>(request) ?? {};
    state.settingValues[key] = payload.value ?? '';
    await json(route, 200, {
      key,
      value: state.settingValues[key],
      updatedAtUtc: '2027-05-15T12:25:00Z',
      updatedByUserId: adminSession.userId
    });
    return;
  }

  await json(route, 404, { details: `No Playwright mock registered for ${method} ${path}.` });
}

function readJsonBody<T>(request: Route['request'] extends () => infer R ? R : never): T | null {
  try {
    return request.postDataJSON() as T;
  } catch {
    return null;
  }
}

async function json(route: Route, status: number, body: unknown): Promise<void> {
  await route.fulfill({
    status,
    contentType: 'application/json',
    body: JSON.stringify(body)
  });
}