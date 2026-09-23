import api from './client';

export interface TrendSummary {
  totalDestinations: number;
  catalogSearches: number;
  catalogSearchesGrowth: number;
  searchToBookingRate: number;
  searchToBookingGrowth: number;
  avgTripBudget: number;
  avgTripBudgetGrowth: number;
  underSuppliedCount: number;
  underSuppliedRegions: string[];
}

export interface DemandPoint {
  month: string;
  bookings: number;
  isCurrent: boolean;
}

export interface PreferenceShare {
  tag: string;
  percentage: number;
}

export interface RegionalPerformance {
  region: string;
  count: number;
  destinationSearches: number;
  bookings: number;
  conversion: number;
  wowChange: number;
}

export interface TrendsData {
  summary: TrendSummary;
  demandCurve: DemandPoint[];
  preferenceShare: PreferenceShare[];
  regionalPerformance: RegionalPerformance[];
}

export const trendsApi = {
  get: () => api.get<TrendsData>('/destinations/trends'),
};
