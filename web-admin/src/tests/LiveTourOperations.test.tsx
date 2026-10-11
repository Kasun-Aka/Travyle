import { render, screen, waitFor } from '@testing-library/react';
import LiveTourOperations from '../pages/LiveTourOperations';
import { vi, describe, it, expect, beforeEach } from 'vitest';

// Mock react-leaflet to prevent Leaflet core from running in jsdom
vi.mock('react-leaflet', () => ({
  MapContainer: ({ children }: any) => (
    <div data-testid="map-container">{children}</div>
  ),
  TileLayer: () => <div data-testid="tile-layer" />,
  Marker: ({ children }: any) => (
    <div data-testid="marker">{children}</div>
  ),
  Popup: ({ children }: any) => (
    <div data-testid="popup">{children}</div>
  ),
  Circle: () => <div data-testid="circle" />,
}));

globalThis.fetch = vi.fn();

describe('LiveTourOperations', () => {
  beforeEach(() => {
    vi.resetAllMocks();
  });

  it('renders loading state initially', () => {
    (globalThis.fetch as any).mockResolvedValueOnce({
      json: () => Promise.resolve({
        stats: {},
        activeTours: [],
        alerts: [],
      }),
      ok: true,
    });

    render(<LiveTourOperations />);

    expect(
      screen.getByText(/Loading Live Operations.../i)
    ).toBeInTheDocument();
  });

  it('renders dashboard with data', async () => {
    const mockData = {
      stats: {
        toursInProgress: 5,
        flagged: 1,
      },
      activeTours: [
        {
          id: '1',
          name: 'Test Tour',
          guide: 'Alice',
          status: 'On Time',
          location: 'Colombo',
          progress: 2,
          progressPercent: 50,
          lastPing: 'Just now',
        },
      ],
      alerts: [],
    };

    const mockRouteLog = {
      tourName: 'Test Tour',
      guideName: 'Alice',
      hasDisruption: false,
      stops: [],
    };

    // First fetch: live operations data
    (globalThis.fetch as any).mockResolvedValueOnce({
      json: () => Promise.resolve(mockData),
      ok: true,
    });

    // Second fetch: route log for the automatically selected tour
    (globalThis.fetch as any).mockResolvedValueOnce({
      json: () => Promise.resolve(mockRouteLog),
      ok: true,
    });

    render(<LiveTourOperations />);

    await waitFor(() => {
      expect(
        screen.getByText('Live tour operations')
      ).toBeInTheDocument();

      expect(
        screen.getByText('Test Tour')
      ).toBeInTheDocument();
    });
  });
});