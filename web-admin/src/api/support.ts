import api from './client';

export interface TourRatingSummary {
  tourId: string;
  tourTitle: string;
  averageRating: number;
  reviewCount: number;
}

export interface SupportAnalytics {
  averageSentimentScore: number;
  activeVouchersCount: number;
  redeemedVouchersCount: number;
  tourAverageRatings: TourRatingSummary[];
}

export interface ReviewItem {
  id: string;
  userId: string;
  userName?: string;
  tourId: string;
  rating: number;
  comment: string;
  isVerified: boolean;
  createdAt: string;
}

export interface UserSupportActivity {
  userId: string;
  userName?: string;
  userEmail?: string;
  tickets: any[];
  reviews: ReviewItem[];
}

export const supportApi = {
  getAnalytics: () => api.get<SupportAnalytics>('/support/analytics'),
  getUserActivity: (userId: string) => api.get<UserSupportActivity>(`/support/users/${userId}/activity`),
};
