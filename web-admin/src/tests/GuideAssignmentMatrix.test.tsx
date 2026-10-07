import { render, screen, waitFor } from '@testing-library/react';
import GuideAssignmentMatrix from '../pages/GuideAssignmentMatrix';
import { vi, describe, it, expect, beforeEach } from 'vitest';

globalThis.fetch = vi.fn();

describe('GuideAssignmentMatrix', () => {
  beforeEach(() => {
    vi.resetAllMocks();
  });

  it('renders loading state initially', () => {
    (globalThis.fetch as any).mockResolvedValueOnce({
      json: () => Promise.resolve({ guides: [], roster: [] }),
      ok: true,
    });
    
    render(<GuideAssignmentMatrix />);
    expect(screen.getByText(/Loading Matrix.../i)).toBeInTheDocument();
  });

  it('renders matrix table after fetching', async () => {
    const mockData = {
      guides: [
        {
          id: '1',
          name: 'Jane Smith',
          meta: 'Verified',
          schedule: Array(7).fill({ label: '-', scheduleId: null })
        }
      ],
      roster: []
    };

    (globalThis.fetch as any).mockResolvedValueOnce({
      json: () => Promise.resolve(mockData),
      ok: true,
    });

    render(<GuideAssignmentMatrix />);

    await waitFor(() => {
      expect(screen.getByText('Guide assignment matrix')).toBeInTheDocument();
      expect(screen.getByText('Jane Smith')).toBeInTheDocument();
    });
  });
});
