import type { PageServerLoad } from './$types';
import { error } from '@sveltejs/kit';

export const load: PageServerLoad = async (event) => {
  const { params } = event;
  const id = Number(params.id);

  if (!Number.isFinite(id) || id <= 0) {
    throw error(404, 'Ticket not found');
  }

  return { ticketId: id };
};