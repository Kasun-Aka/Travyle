import api from './client';

export interface TrendSummary {
  totalPackages: number;
  totalRegions: number;
  totalUniqueTags: number;
  underSuppliedCount: number;
  underSuppliedRegions: string[];
  coverageScore: number;
  avgRatingOverall: number;
  catalogSearches: number;
  searchToBookingRate: number;
  avgTripBudget: number;
}

export interface RegionStat {
  region: string;
  count: number;
  avgRating: number;
  percentage: number;
}

export interface TagStat {
  tag: string;
  count: number;
  percentage: number;
}

export interface MonthlyAdded {
  month: string;
  count: number;
}

export interface NewestPackage {
  id: string;
  name: string;
  region: string;
  tags: string[];
  averageRating: number;
  addedAgo: string;
}

export interface DemandPoint {
  month: string;
  bookings: number;
  isCurrent: boolean;
}

export interface TrendsData {
  generatedAt: string;
  summary: TrendSummary;
  byRegion: RegionStat[];
  tagFrequency: TagStat[];
  addedByMonth: MonthlyAdded[];
  newestPackages: NewestPackage[];
  demandCurve: DemandPoint[];
}

export const trendsApi = {
  get: (months: number = 6) => api.get<TrendsData>(`/destinations/trends?months=${months}`),
};
