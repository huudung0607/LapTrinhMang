using Microsoft.EntityFrameworkCore;
using MongoDB.EntityFrameworkCore.Extensions;
using NetworkProgramming.Models;

namespace NetworkProgramming.Data;

/// <summary>DbContext cho database MongoDB "App-chat".</summary>
public class appDbContext(DbContextOptions<appDbContext> options) : DbContext(options)
{
    public DbSet<AdminAccount> AdminAccounts => Set<AdminAccount>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<GroupMember> GroupMembers => Set<GroupMember>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<MessageReaction> MessageReactions => Set<MessageReaction>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        // Tên collection khớp với MongoDB Compass.
        b.Entity<AdminAccount>().ToCollection("account-admin");
        b.Entity<User>().ToCollection("users");
        b.Entity<Conversation>().ToCollection("conversations");
        b.Entity<GroupMember>().ToCollection("group-members");
        b.Entity<Message>().ToCollection("messages");
        b.Entity<Attachment>().ToCollection("attachments");
        b.Entity<MessageReaction>().ToCollection("message-reactions");
    }
}
