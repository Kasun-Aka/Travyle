import { render, screen, waitFor } from '@testing-library/react';
import StaffAccessControl from '../pages/StaffAccessControl';
import { vi, describe, it, expect, beforeEach } from 'vitest';

// Mock fetch
globalThis.fetch = vi.fn();

describe('StaffAccessControl', () => {
  beforeEach(() => {
    vi.resetAllMocks();
  });

  it('renders loading state initially', () => {
    (globalThis.fetch as any).mockResolvedValueOnce({
      json: () => Promise.resolve([]),
      ok: true,
    });
    
    render(<StaffAccessControl />);
    expect(screen.getByText(/Loading staff data.../i)).toBeInTheDocument();
  });

  it('renders staff list after fetching', async () => {
    const mockStaff = [
      { id: '1', fullName: 'John Doe', firebaseUid: 'uid123', email: 'john@test.com', role: 'Guide', createdAt: new Date().toISOString() },
    ];

    (globalThis.fetch as any).mockResolvedValueOnce({
      json: () => Promise.resolve(mockStaff),
      ok: true,
    });

    render(<StaffAccessControl />);

    await waitFor(() => {
      expect(screen.getByText('John Doe')).toBeInTheDocument();
      expect(screen.getByText('Guide')).toBeInTheDocument();
    });
  });
});
