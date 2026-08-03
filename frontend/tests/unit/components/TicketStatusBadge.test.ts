import { render } from '@testing-library/svelte';
import { describe, it, expect } from 'vitest';
import TicketStatusBadge from '$lib/components/TicketStatusBadge.svelte';
import { TicketStatus } from '$lib/types/enums';

describe('TicketStatusBadge', () => {
  it('renders New status with correct badge', () => {
		const { container } = render(TicketStatusBadge, { props: { status: TicketStatus.New } });
		const badge = container.querySelector('.badge');
    expect(badge).toHaveClass('bg-status-new');
  });

  it('renders Open status with correct badge', () => {
		const { container } = render(TicketStatusBadge, { props: { status: TicketStatus.Open } });
		const badge = container.querySelector('.badge');
    expect(badge).toHaveClass('bg-status-open');
  });

  it('renders InProcess status with correct badge', () => {
		const { container } = render(TicketStatusBadge, { props: { status: TicketStatus.InProcess } });
		const badge = container.querySelector('.badge');
    expect(badge).toHaveClass('bg-status-inprocess');
  });

  it('renders Resolved status with correct badge', () => {
		const { container } = render(TicketStatusBadge, { props: { status: TicketStatus.Resolved } });
		const badge = container.querySelector('.badge');
    expect(badge).toHaveClass('bg-status-resolved');
  });

  it('renders Cancelled status with correct badge', () => {
		const { container } = render(TicketStatusBadge, { props: { status: TicketStatus.Cancelled } });
		const badge = container.querySelector('.badge');
    expect(badge).toHaveClass('bg-status-cancelled');
  });

  it('renders Postponed status with correct badge', () => {
		const { container } = render(TicketStatusBadge, { props: { status: TicketStatus.Postponed } });
		const badge = container.querySelector('.badge');
    expect(badge).toHaveClass('bg-status-postponed');
  });

  it('renders Returned status with correct badge', () => {
		const { container } = render(TicketStatusBadge, { props: { status: TicketStatus.Returned } });
		const badge = container.querySelector('.badge');
    expect(badge).toHaveClass('bg-status-returned');
  });

  it('has aria-label attribute for accessibility', () => {
		const { container } = render(TicketStatusBadge, { props: { status: TicketStatus.New } });
    const badge = container.querySelector('.badge');
    
    expect(badge).toHaveAttribute('aria-label');
  });
});
