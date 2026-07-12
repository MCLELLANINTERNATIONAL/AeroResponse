using AeroResponse.Data;
using AeroResponse.Models;

namespace AeroResponse.Repositories;

public class MembershipRepository(ApplicationDbContext context)
    : EfGenericRepository<Membership>(context);