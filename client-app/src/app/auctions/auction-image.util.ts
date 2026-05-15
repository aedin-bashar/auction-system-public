export const DEFAULT_AUCTION_IMAGE_URL = '/assets/images/auction-placeholder.svg';

export function resolveAuctionImageUrl(
  apiOrigin: string,
  auctionId: string,
  primaryImageId: string | null | undefined
): string {
  if (!primaryImageId || primaryImageId.trim().length === 0) {
    return DEFAULT_AUCTION_IMAGE_URL;
  }

  return `${apiOrigin}/api/auctions/${auctionId}/images/${primaryImageId}`;
}

export function setDefaultAuctionImage(event: Event): void {
  const element = event.target;
  if (!(element instanceof HTMLImageElement)) {
    return;
  }

  if (element.src.endsWith(DEFAULT_AUCTION_IMAGE_URL)) {
    return;
  }

  element.src = DEFAULT_AUCTION_IMAGE_URL;
}
